using StateBased.ConsistentMessaging.Console.Infrastructure;

namespace StateBased.ConsistentMessaging.Console
{
    class LeaderBoard
    {
        public LeaderBoardData Data { get; set; }

        public void Handle(IHandlerContext context, Hit @event)
        {
            Data.Apply(new TargetHit());
        }

        public void Handle(IHandlerContext context, Missed @event)
        {
            Data.Apply(new TargetMissed());
        }

        public class LeaderBoardData : EventSourcedData
        {
            public int NumberOfHits { get; set; }
            public int NumberOfMisses { get; set; }

            public void When(TargetHit @event)
            {
                NumberOfHits++;
            }

            public void When(TargetMissed @event)
            {
                NumberOfMisses++;
            }
        }

        public class TargetHit {}

        public class TargetMissed {}
    }
}