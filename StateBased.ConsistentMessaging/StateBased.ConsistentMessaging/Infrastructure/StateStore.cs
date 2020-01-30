using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;
using Streamstone;

namespace StateBased.ConsistentMessaging.Infrastructure
{
    class StateStore
    {
        readonly CloudTable table;

        public StateStore(CloudTable table)
        {
            this.table = table;
        }

        public async Task<(THandlerState, Stream, bool)> LoadState<THandlerState>(Guid stateId, Guid messageId) where THandlerState : EventSourcedState, new()
        {
            var streamId = $"{typeof(THandlerState).Name}-{stateId}";
            var partition = new Partition(table, streamId);

            var state = new THandlerState();

            var existent = await Stream.TryOpenAsync(partition);

            if (existent.Found == false)
            {
                var createdStream = await Stream.ProvisionAsync(partition);

                return (state, createdStream, false);
            }

            var isDuplicate = false;

            var stream = await ReadStream(partition, properties =>
            {
                var mId = properties["MessageId"].GuidValue;
                var @event = DeserializeEvent(properties);

                if (mId == messageId)
                {
                    isDuplicate = true;
                } 
                else if (@event != null)
                {
                    state.Apply(@event);
                }

                return isDuplicate;
            });

            state.Changes.Clear();

            return (state, stream, isDuplicate);

            
        }

        object DeserializeEvent(EventProperties properties)
        {
            var data = properties["Data"].StringValue;
            var type = properties.ContainsKey("Type") ? Type.GetType(properties["Type"].StringValue) : null;

            var @event = type != null ? JsonConvert.DeserializeObject(data, type) : null;
            return @event;
        }

        static async Task<Stream> ReadStream(Partition partition, Func<EventProperties, bool> process)
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

        public Task UpdateState(Stream stream, List<object> changes, Guid messageId)
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