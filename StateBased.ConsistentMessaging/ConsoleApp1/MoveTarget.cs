using System;

namespace StateBased.ConsistentMessaging.Console
{
    class MoveTarget
    {
        public Guid AttemptId { get; set; }

        public int Position { get; set; }
    }
}