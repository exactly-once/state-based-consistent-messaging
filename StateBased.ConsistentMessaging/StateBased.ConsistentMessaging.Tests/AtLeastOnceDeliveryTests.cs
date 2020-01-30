using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Raw;
using NUnit.Framework;
using StateBased.ConsistentMessaging.Domain;
using StateBased.ConsistentMessaging.Infrastructure;

namespace StateBased.ConsistentMessaging.Tests
{
    public class AtLeastOnceDeliveryTests
    {
        private IReceivingRawEndpoint endpoint;
        private StateStore storage;
        private ConcurrentDictionary<Guid, (TaskCompletionSource<bool> cs, int pending)> runs;

        [SetUp]
        public async Task Setup()
        {
            runs = new ConcurrentDictionary<Guid, (TaskCompletionSource<bool>, int)>();

            (endpoint, storage) = await EndpointBuilder.SetupEndpoint((runId, _, output) =>
            {
                var pendingDelta = output.Length - 1;

                var value = runs.AddOrUpdate(
                    runId,
                    _ => throw new Exception("Should not happen"),
                    (id, v) => (v.cs, v.pending + pendingDelta));

                if (value.pending == 0)
                {
                    runs.TryRemove(runId, out var _);

                    value.cs.SetResult(true);
                }
            });
        }

        async Task DispatchAndWait(Message[] messages)
        {
            var runId = Guid.NewGuid();
            var cs = new TaskCompletionSource<bool>();

            runs.AddOrUpdate(
                runId,
                _ => (cs, messages.Length),
                (_, __) => throw new Exception("This should not happen"));

            await Task.WhenAll(messages.Select(m => endpoint.Send(m, runId)).ToArray());

            await cs.Task;
        }

        [Test]
        public async Task ScenarioA()
        {
            var gameId = Guid.NewGuid();

            var move = new MoveTarget{Id = Guid.NewGuid(), GameId = gameId, Position = 1};
            var fire = new FireAt {Id = Guid.NewGuid(), GameId = gameId, Position = 1};

            await DispatchAndWait(new Message[] { move });
            await DispatchAndWait(new Message[] { fire, fire });

            var (leaderBoard, _, __) = await storage.LoadState<LeaderBoard.LeaderBoardData>(gameId, Guid.Empty);

            Assert.AreEqual(1, leaderBoard.NumberOfHits);
        }

        [Test]
        public async Task ScenarioB()
        {
            var gameId = Guid.NewGuid();

            var firstMove = new MoveTarget{Id = Guid.NewGuid(), GameId = gameId, Position = 1};
            var fire = new FireAt {Id = Guid.NewGuid(), GameId = gameId, Position = 1};
            var secondMove = new MoveTarget{Id = Guid.NewGuid(), GameId = gameId, Position = 2};

            await DispatchAndWait(new Message[] { firstMove });
            await DispatchAndWait(new Message[] { fire });
            await DispatchAndWait(new Message[] { secondMove });
            await DispatchAndWait(new Message[] { fire });

            var (leaderBoard, _, __) = await storage.LoadState<LeaderBoard.LeaderBoardData>(gameId, Guid.Empty);

            Assert.AreEqual(1, leaderBoard.NumberOfHits);
        }

        [Test]
        public async Task ScenarioC()
        {
            var gameId = Guid.NewGuid();

            var move = new MoveTarget{Id = Guid.NewGuid(), GameId = gameId, Position = 1};
            var fire = new FireAt {Id = Guid.NewGuid(), GameId = gameId, Position = 1};
            var secondMove = new MoveTarget{Id = Guid.NewGuid(), GameId = gameId, Position = 2};

            await DispatchAndWait(new Message[] { move });
            await DispatchAndWait(new Message[] { fire, fire, secondMove });

            var (leaderBoard, _, __) = await storage.LoadState<LeaderBoard.LeaderBoardData>(gameId, Guid.Empty);
            
            var hits = leaderBoard.NumberOfHits;
            var misses = leaderBoard.NumberOfMisses;

            Assert.AreEqual(hits + misses, 1);
        }
    }
}