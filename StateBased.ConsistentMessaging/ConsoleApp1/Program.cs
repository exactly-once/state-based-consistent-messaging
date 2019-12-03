using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Marten;
using Marten.Events;
using NServiceBus;
using NServiceBus.Raw;
using StateBased.ConsistentMessaging.Console.Infrastructure;

[assembly: InternalsVisibleTo("StateBased.ConsistentMessaging.Tests")]

namespace StateBased.ConsistentMessaging.Console
{
    class Program
    {
        public const string EndpointName = "TheEndpoint";

        static async Task Main(string[] args)
        {
            var (endpoint, _) = await SetupEndpoint();

            var gameId = Guid.NewGuid();

            while (true)
            {
                var commandText = System.Console.ReadLine();

                if (ConsoleCommand.TryParse(commandText, out var command))
                {
                    if (command.Type == ConsoleCommand.CommandType.FireAt)
                    {
                        await endpoint.Send(new FireAt
                        {
                            Id = Guid.NewGuid(), 
                            GameId = gameId,
                            Position = command.Value
                        } );
                    }

                    if (command.Type == ConsoleCommand.CommandType.Move)
                    {
                        await endpoint.Send(new MoveTarget
                        {
                            Id = Guid.NewGuid(),
                            GameId = gameId,
                            Position = command.Value
                        });
                    }
                }
            }
        }

        internal static async Task<(IReceivingRawEndpoint, SagaStore)> SetupEndpoint()
        {
            IReceivingRawEndpoint endpoint = null;
            
            var documentStore = DocumentStore.For(_ =>
            {
                _.Connection(@"User ID=postgres;Password=yourPassword;Host=localhost;Port=5432;Database=exactly-once;");
                _.Events.StreamIdentity = StreamIdentity.AsString;
            });

            var sagaStore = new SagaStore(documentStore);

            var endpointConfiguration = RawEndpointConfiguration.Create(
                endpointName: EndpointName,
                onMessage: (c, d) => HandlerInvoker.OnMessage(c, sagaStore, endpoint),
                poisonMessageQueue: "error");

            endpointConfiguration.UseTransport<LearningTransport>();

            endpoint =  await RawEndpoint.Start(endpointConfiguration);

            return (endpoint, sagaStore);
        }

        class ConsoleCommand
        {
            public int Value { get; set; }

            public CommandType Type { get; set; }

            public static bool TryParse(string commandText, out ConsoleCommand command)
            {
                command = new ConsoleCommand {Type = CommandType.Unknown};;

                if (commandText == null || commandText.Length < 2)
                {
                    return false;
                }

                var commandType = commandText[0];

                if (!int.TryParse(commandText.Substring(1), out var commandValue))
                {
                    return false;
                }

                command = new ConsoleCommand
                {
                    Value = commandValue,
                    Type = commandType == 'f' ? CommandType.FireAt : CommandType.Move
                };

                return true;
            }

            public enum CommandType
            {
                Move,
                FireAt,
                Unknown
            }
        }
    }
}