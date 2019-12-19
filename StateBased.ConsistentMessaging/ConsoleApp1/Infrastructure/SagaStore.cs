using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;
using NServiceBus;
using Streamstone;

namespace StateBased.ConsistentMessaging.Console.Infrastructure
{
    class SagaStore
    {
        readonly CloudTable table;

        public SagaStore(CloudTable table)
        {
            this.table = table;
        }

        public async Task<(TSagaData, Stream, bool)> LoadSaga<TSagaData>(Guid sagaId, Guid messageId) where TSagaData : EventSourcedData, new()
        {
            var streamId = $"{typeof(TSagaData).Name}-{sagaId}";
            var partition = new Partition(table, streamId);

            var saga = new TSagaData();

            var existent = await Stream.TryOpenAsync(partition);

            if (existent.Found == false)
            {
                var createdStream = await Stream.ProvisionAsync(partition);

                return (saga, createdStream, false);
            }

            var isDuplicate = false;

            var stream = await ReadStream(partition, properties =>
            {
                var mId = Guid.Parse(properties["MessageId"].StringValue);
                var data = properties["Data"].StringValue;
                var type = Type.GetType(properties["Type"].StringValue);

                var @event = JsonConvert.DeserializeObject(data, type);

                if (mId != messageId && isDuplicate)
                {
                    return true;
                }

                if (mId == messageId)
                {
                    isDuplicate = true;
                }

                if (@event != null)
                {
                    saga.Apply(@event);
                }

                return isDuplicate;
            });

            saga.Changes.Clear();

            return (saga, stream, isDuplicate);
        }

        private static async Task<Stream> ReadStream(Partition partition, Func<EventProperties, bool> process)
        {
            StreamSlice<EventProperties> slice;

            var nextSliceStart = 1;
            var sliceSize = 1;

            while(true)
            {
                slice = await Stream.ReadAsync(partition, nextSliceStart, sliceSize);

                foreach (var @event in slice.Events)
                {
                    if (process(@event)) break;
                }

                nextSliceStart += nextSliceStart + slice.Events.Length;
            }

            return slice.Stream;
        }

        public Task UpdateSaga(Stream stream, List<object> changes, Guid messageId)
        {
            if (changes.Count == 0)
            {
                changes.Add(null);
            }

            var events = changes.Select(c =>
            {
                var eventId = EventId.From(Guid.NewGuid());

                var properties = EventProperties.From(new
                {
                    MessageId = messageId,
                    Type = c.GetType().FullName,
                    Data = JsonConvert.SerializeObject(c)
                });

                return new EventData(eventId, properties);
            }).ToArray();

            return Stream.WriteAsync(stream, events);
        }
    }
}