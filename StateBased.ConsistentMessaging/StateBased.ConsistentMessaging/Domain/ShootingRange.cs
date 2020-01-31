﻿using StateBased.ConsistentMessaging.Infrastructure;

namespace StateBased.ConsistentMessaging.Domain
{
    class ShootingRange
    {
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
        }

        public void Handle(IHandlerContext context, MoveTarget command)
        {
            Data.Apply(new TargetMoved{Position = command.Position});
        }

        public class ShootingRangeData : EventSourcedState
        {
            public int TargetPosition { get; set; }

            public void When(TargetMoved @event)
            {
                TargetPosition = @event.Position;
            }
        }

        public class TargetMoved
        {
            public int Position { get; set; }
        }
    }
}