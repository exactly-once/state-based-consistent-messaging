using StateBased.ConsistentMessaging.Console.Infrastructure;

namespace StateBased.ConsistentMessaging.Console
{
    class LeaderBoard
    {
        public LeaderBoardData Data { get; set; }

        public void Handle(IHandlerContext context, Hit @event)
        {
            Data.Apply(new HitRecorded());
        }

        public void Handle(IHandlerContext context, Missed @event)
        {
            Data.Apply(new MissRecorded());
        }

        public void Print() => System.Console.WriteLine($"Hits: {Data.NumberOfHits}\tMisses: {Data.NumberOfMisses}");

        public class LeaderBoardData : EventSourcedData
        {
            public int NumberOfHits { get; set; }
            public int NumberOfMisses { get; set; }

            public void When(HitRecorded @event)
            {
                NumberOfHits++;
            }

            public void When(MissRecorded @event)
            {
                NumberOfMisses++;
            }
        }

        public class HitRecorded { }

        public class MissRecorded{}
    }
}