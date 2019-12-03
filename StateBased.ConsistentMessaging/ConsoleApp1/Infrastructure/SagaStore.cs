using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Marten;
using Marten.Events;

namespace StateBased.ConsistentMessaging.Console.Infrastructure
{
    class SagaStore
    {
        readonly DocumentStore documentStore;

        public SagaStore(DocumentStore documentStore)
        {
            this.documentStore = documentStore;
        }

        public async Task<(TSagaData, int, bool)> LoadSaga<TSagaData>(Guid sagaId, Guid messageId) where TSagaData : EventSourcedData, new()
        {
            var streamId = $"{typeof(TSagaData).Name}-{sagaId}";

            var stream = await ReadStream(streamId);
            var version = stream.Count;

            var isDuplicate = false;

            var saga = new TSagaData();

            foreach (var @event in stream)
            {
                var correlatedEvent = (MessageCorrelatedEvent) @event.Data;

                if (correlatedEvent.MessageId != messageId && isDuplicate)
                {
                    break;
                }

                if (correlatedEvent.MessageId == messageId)
                {
                    isDuplicate = true;
                }

                if (correlatedEvent.Change != null)
                {
                    saga.Apply(correlatedEvent.Change);
                }
            }

            saga.Changes.Clear();

            return (saga, version, isDuplicate);
        }

        public async Task UpdateSaga<TSagaData>(Guid sagaId, int version, List<object> changes, Guid messageId)
        {
            var streamId = $"{typeof(TSagaData).Name}-{sagaId}";

            var correlatedChanges = changes.Select(c => new MessageCorrelatedEvent
            {
                Change = c,
                MessageId = messageId
            }).ToList();

            if (correlatedChanges.Count == 0)
            {
                correlatedChanges.Add(new MessageCorrelatedEvent{MessageId = messageId});
            }

            using var session = documentStore.OpenSession();
            
            var eventStore = session.Events;

            var expectedVersion = version + correlatedChanges.Count;
            
            eventStore.Append(streamId, expectedVersion, correlatedChanges.ToArray());

            await session.SaveChangesAsync();
        }

        private async Task<IReadOnlyCollection<IEvent>> ReadStream(string streamId)
        {
            IReadOnlyCollection<IEvent> stream;
            using (var session = documentStore.OpenSession())
            {
                var eventStore = session.Events;

                stream = await eventStore.FetchStreamAsync(streamId);
            }

            return stream;
        }

        class MessageCorrelatedEvent
        {
            public object Change { get; set; }
            public Guid MessageId { get; set; }
        }
    }
}