using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using StateBased.ConsistentMessaging.Domain;
using StateBased.ConsistentMessaging.Infrastructure;

[assembly: InternalsVisibleTo("StateBased.ConsistentMessaging.Tests")]

namespace StateBased.ConsistentMessaging
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var (endpoint, _) = await EndpointBuilder.SetupEndpoint((_, __, ___) => {});

            var gameId = Guid.NewGuid();

            new ConsoleRunner(new Dictionary<char, Func<int, Task>>
            {
                {'f', v => endpoint.Send(new FireAt {Id = Guid.NewGuid(), GameId = gameId, Position = v} )},
                {'s', v => endpoint.Send(new StartNewRound {Id = Guid.NewGuid(), GameId = gameId, Position = v})}
            }).Run();
        }
    }

    class ConsoleRunner
    {
        private readonly Dictionary<char, Func<int, Task>> commands;

        public ConsoleRunner(Dictionary<char, Func<int, Task>> commands)
        {
            this.commands = commands;
        }

        public void Run()
        {
            while (true)
            {
                var input = Console.ReadLine();

                if (TryParse(input, out var code, out var value))
                {
                    commands.Keys.ToList().ForEach(k =>
                    {
                        if (k == code)
                        {
                            commands[k].Invoke(value).GetAwaiter().GetResult();
                        }
                    });
                }
            }
        }

        static bool TryParse(string input, out char command, out int value)
        {
            command = 'x';
            value = 0;

            if (input == null || input.Length < 2)
            {
                return false;
            }

            command = input[0];

            if (!int.TryParse(input.Substring(1), out value))
            {
                return false;
            }

            return true;
        }
    }
}