using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Raw;
using StateBased.ConsistentMessaging.Console.Infrastructure;

namespace StateBased.ConsistentMessaging.Console
{
    class Program
    {
        public const string EndpointName = "TheEndpoint";

        static async Task Main(string[] args)
        {
            var sagaStore = new SagaStore();
            IReceivingRawEndpoint endpoint = null;

            var endpointConfiguration = RawEndpointConfiguration.Create(
                endpointName: EndpointName,
                onMessage: (c,d) => HandlerInvoker.OnMessage(c, sagaStore, endpoint),
                poisonMessageQueue: "error");
            endpointConfiguration.UseTransport<LearningTransport>();

            endpoint = await RawEndpoint.Start(endpointConfiguration);

            while (true)
            {
                var command = System.Console.ReadKey();

                switch (command.Key)
                {
                    case ConsoleKey.F : 
                        await endpoint.Send(new FireAt { AttemptId = Guid.NewGuid(), Position = 42 } );
                        break;
                    case ConsoleKey.M :
                        await endpoint.Send(new MoveTarget { Position = 1 });
                        break;
                }
            }
            

            System.Console.WriteLine("Hello World!");
            System.Console.ReadLine();
        }
    }
}
