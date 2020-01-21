using System;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using NServiceBus;
using NServiceBus.Logging;
using NServiceBus.Raw;
using StateBased.ConsistentMessaging.Domain;

namespace StateBased.ConsistentMessaging.Infrastructure
{
    class EndpointBuilder
    {
        public const string EndpointName = "TheEndpoint";

        static async Task<CloudTable> PrepareStorageTable()
        {
            var table = CloudStorageAccount
                .DevelopmentStorageAccount
                .CreateCloudTableClient()
                .GetTableReference(EndpointName);

            await table.DeleteIfExistsAsync();
            await table.CreateIfNotExistsAsync();

            return table;
        }

        internal static async Task<(IReceivingRawEndpoint, SagaStore)> SetupEndpoint(Action<Guid, Message, Message[]> messageProcessed)
        {
            var storageTable = await PrepareStorageTable();

            var sagaStore = new SagaStore(storageTable);
            var handlerInvoker = new HandlerInvoker(sagaStore);

            var endpointConfiguration = RawEndpointConfiguration.Create(
                endpointName: EndpointName,
                onMessage: async (c, d) =>
                {
                    var message = Serializer.Deserialize(c.Body, c.Headers);

                    var outputMessages = await handlerInvoker.Process(message);

                    var runId = Guid.Parse(c.Headers["Message.RunId"]);

                    messageProcessed(runId, message, outputMessages);

                    await d.Send(outputMessages, runId);
                },
                poisonMessageQueue: "error");

            endpointConfiguration.UseTransport<LearningTransport>()
                .Transactions(TransportTransactionMode.ReceiveOnly);

            var defaultFactory = LogManager.Use<DefaultFactory>();
            defaultFactory.Level(LogLevel.Debug);

            var endpoint =  await RawEndpoint.Start(endpointConfiguration);

            return (endpoint, sagaStore);
        }
    }
}