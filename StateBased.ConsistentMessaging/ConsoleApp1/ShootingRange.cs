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
                context.Publish(new Hit{AttemptId = command.AttemptId});
            }
            else
            {
                context.Publish(new Missed{AttemptId = command.AttemptId});
            }
        }

        public void Handle(IHandlerContext context, MoveTarget command)
        {
            TargetPosition = command.Position;
        }
    }
}