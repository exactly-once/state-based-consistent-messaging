using System;

namespace StateBased.ConsistentMessaging.Console
{
    class FireAt : Message
    {
        public Guid GameId { get; set; }
        public int Position { get; set; }
    }

    class MoveTarget : Message
    {
        public Guid GameId { get; set; }
        public int Position { get; set; }
    }

    class Hit : Message
    {
        public Guid GameId { get; set; }
    }

    class Missed : Message
    {
        public Guid GameId { get; set; } 
    }

    class Message
    {
        public Guid Id { get; set; }
    }
}