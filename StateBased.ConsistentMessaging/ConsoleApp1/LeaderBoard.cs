using StateBased.ConsistentMessaging.Console.Infrastructure;

namespace StateBased.ConsistentMessaging.Console
{
    class LeaderBoard
    {
        public int NumberOfHits { get; set; }
        public int NumberOfMisses { get; set; }

        public void Handle(IHandlerContext context, Hit @event)
        {
            NumberOfHits++;

            PrintStats();
        }

        public void Handle(IHandlerContext context, Missed @event)
        {
            NumberOfMisses++;

            PrintStats();
        }

        void PrintStats()
        {
            System.Console.WriteLine($"Hits: {NumberOfHits}\tMisses: {NumberOfMisses}");
        }
    }
}