using System;

namespace StateBased.ConsistentMessaging.Console
{
    class FireAt
    {
        public Guid AttemptId { get; set; }
        public int Position { get; set; }
    }
}