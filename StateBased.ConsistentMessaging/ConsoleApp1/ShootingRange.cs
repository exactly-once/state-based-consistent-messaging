using System;
using StateBased.ConsistentMessaging.Console.Infrastructure;

namespace StateBased.ConsistentMessaging.Console
{
    class ShootingRange
    {
        public int TargetPosition { get; set; }

        public void Handle(IHandlerContext context, FireAt command)
        {
            if (TargetPosition == command.Position)
            {
                context.Publish(new Hit
                {
                    Id = Guid.NewGuid(),
                    GameId = command.GameId
                });
            }
            else
            {
                context.Publish(new Missed
                {
                    Id = Guid.NewGuid(),
                    GameId = command.GameId
                });
            }
        }

        public void Handle(IHandlerContext context, MoveTarget command)
        {
            TargetPosition = command.Position;
        }
    }
}