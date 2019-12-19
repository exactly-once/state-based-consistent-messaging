using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Marten;
using Marten.Events;
using NUnit.Framework;

namespace StateBased.ConsistentMessaging.Tests
{
    class MartenSandbox
    {

        [Test]
        public async Task OptimisticConcurrencyCheck()
        {
            var documentStore = DocumentStore.For(_ =>
            {
                _.Connection(@"User ID=postgres;Password=yourPassword;Host=localhost;Port=5432;Database=exactly-once;");
                _.Events.StreamIdentity = StreamIdentity.AsString;
            });

            var streamId = "test-stream";

            using var session = documentStore.OpenSession();
            var eventStore = session.Events;

            var stream = await eventStore.FetchStreamAsync(streamId);

            eventStore.Append(streamId, 1, new []{new SampleEvent() });

            await session.SaveChangesAsync();
        }
    }

    class SampleEvent{}
}
