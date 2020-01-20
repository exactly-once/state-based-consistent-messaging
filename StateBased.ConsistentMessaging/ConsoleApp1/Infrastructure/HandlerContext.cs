using System.Collections.Generic;

namespace StateBased.ConsistentMessaging.Console.Infrastructure
{
    class HandlerContext : IHandlerContext
    {
        public List<Message> Messages { get; set; } = new List<Message>();

        public void Publish(Message message)
        {
            Messages.Add(message);
        }
    }

    internal interface IHandlerContext
    {
        void Publish(Message message);
    }
}