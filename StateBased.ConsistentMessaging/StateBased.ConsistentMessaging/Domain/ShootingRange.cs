using StateBased.ConsistentMessaging.Infrastructure;

namespace StateBased.ConsistentMessaging.Domain
{
    class ShootingRange
    {
        public const int MaxAttemptsInARound = 2;

        public ShootingRangeData Data { get; set; }

        public void Handle(IHandlerContext context, FireAt command)
        {
            if (Data.TargetPosition == command.Position)
            {
                context.Publish(new Hit
                {
                    Id = context.NewGuid(),
                    GameId = command.GameId
                });
            }
            else
            {
                context.Publish(new Missed
                {
                    Id = context.NewGuid(),
                    GameId = command.GameId
                });
            }

            if (Data.NumberOfAttempts + 1 >= MaxAttemptsInARound)
            {
                Data.Apply(new NewRoundStarted { Position = context.Random.Next(0, 100)});
            }
            else
            {
                Data.Apply(new AttemptMade());
            }
        }

        public void Handle(IHandlerContext context, StartNewRound command)
        {
            Data.Apply(new NewRoundStarted{Position = command.Position});
        }

        public class ShootingRangeData : EventSourcedState
        {
            public int TargetPosition { get; set; }

            public int NumberOfAttempts { get; set; }

            public void When(NewRoundStarted @event)
            {
                TargetPosition = @event.Position;
            }

            public void When(AttemptMade @event)
            {
                NumberOfAttempts++;
            }
        }

        public class NewRoundStarted
        {
            public int Position { get; set; }
        }

        public class AttemptMade { }
    }
}