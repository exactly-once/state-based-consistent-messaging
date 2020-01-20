using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;
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
                var mId = properties["MessageId"].GuidValue.GetValueOrDefault();
                var data = properties["Data"].StringValue;
                var type = properties.ContainsKey("Type") ? Type.GetType(properties["Type"].StringValue) : null;

                var @event = type != null ? JsonConvert.DeserializeObject(data, type) : null;

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

            var sliceStart = 1;

            do
            {
                slice = await Stream.ReadAsync(partition, sliceStart, sliceSize: 1);

                foreach (var @event in slice.Events)
                {
                    if (process(@event)) break;
                }

                sliceStart += slice.Events.Length;
            } 
            while (slice.HasEvents);

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
                    Type = c?.GetType().FullName,
                    Data = JsonConvert.SerializeObject(c)
                });

                return new EventData(eventId, properties);
            }).ToArray();

            return Stream.WriteAsync(stream, events);
        }
    }
}