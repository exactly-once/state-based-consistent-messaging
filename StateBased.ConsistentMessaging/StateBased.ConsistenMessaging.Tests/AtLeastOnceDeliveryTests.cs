using System;
using System.Linq;
using System.Threading.Tasks;
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

        [SetUp]
        public async Task Setup()
        {
            (endpoint, storage) = await Program.SetupEndpoint();
        }

        [Test]
        public async Task SimpleDuplication()
        {
            var gameId = Guid.NewGuid();
            var attemptId = Guid.NewGuid();

            await Dispatch(new[]
            {
                new MoveTarget {Id = Guid.NewGuid(), GameId = gameId, Position = 1}
            });

            await WaitFor<ShootingRange.ShootingRangeData>(gameId, version: 1);

            await Dispatch(new[]
            {
                new FireAt {Id = attemptId, GameId = gameId, Position = 1},
                new FireAt {Id = attemptId, GameId = gameId, Position = 1}
            });

            await WaitFor<LeaderBoard.LeaderBoardData>(gameId, 2);

            var (leaderBoard, _) = storage.Get<LeaderBoard.LeaderBoardData>(gameId);

            Assert.AreEqual(1, leaderBoard.NumberOfHits);
        }

        Task Dispatch(Message[] messages) => Task.WhenAll(messages.Select(m => endpoint.Send(m)).ToArray());

        async Task WaitFor<TSaga>(Guid sagaId, int version) where TSaga : new()
        {
            var (_, cVersion) = storage.Get<TSaga>(sagaId);

            while(cVersion != version)
            { 
                await Task.Delay(TimeSpan.FromMilliseconds(100));

                (_, cVersion) = storage.Get<TSaga>(sagaId);
            }
        }
    }
}