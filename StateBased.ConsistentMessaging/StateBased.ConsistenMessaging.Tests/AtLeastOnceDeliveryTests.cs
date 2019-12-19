using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Raw;
using NUnit.Framework;
using StateBased.ConsistentMessaging.Console;
using StateBased.ConsistentMessaging.Console.Infrastructure;

namespace StateBased.ConsistentMessaging.Tests
{
    public class AtLeastOnceDeliveryTests
    {
        private IReceivingRawEndpoint endpoint;
        private SagaStore storage;
        private ConcurrentDictionary<Guid, int> messageProcessed;
        [SetUp]
        public async Task Setup()
        {
            messageProcessed = new ConcurrentDictionary<Guid, int>();

            (endpoint, storage) = await Program.SetupEndpoint(messageId =>
                {
                    messageProcessed.AddOrUpdate(Guid.Parse(messageId), mId => 1, (mId, c) => c + 1);
                });
        }

        [Test]
        public async Task SimpleDuplication()
        {
            var gameId = Guid.NewGuid();
            var attemptId = Guid.NewGuid();
            var moveId = Guid.NewGuid();

            await Dispatch(new[]
            {
                new MoveTarget {Id = moveId, GameId = gameId, Position = 1}
            });

            await WaitFor(moveId, 1);

            await Dispatch(new[]
            {
                new FireAt {Id = attemptId, GameId = gameId, Position = 1},
                new FireAt {Id = attemptId, GameId = gameId, Position = 1}
            });

            await WaitFor(attemptId, 2);

            var (leaderBoard, _, __) = await storage.LoadSaga<LeaderBoard.LeaderBoardData>(gameId, Guid.Empty);

            Assert.AreEqual(1, leaderBoard.NumberOfHits);
        }

        Task Dispatch(Message[] messages) => Task.WhenAll(messages.Select(m => endpoint.Send(m, m.Id)).ToArray());

        async Task WaitFor(Guid messageId, int count)
        {
            var timeout = Debugger.IsAttached ? TimeSpan.MaxValue : TimeSpan.FromSeconds(500);
            var stopwatch = Stopwatch.StartNew();

            while (!messageProcessed.TryGetValue(messageId, out var mCount) && mCount != count)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100));

                if (stopwatch.Elapsed > timeout)
                {
                    throw new Exception($"Timeout on messageId: {messageId}");
                }
            }
        }
    }
}