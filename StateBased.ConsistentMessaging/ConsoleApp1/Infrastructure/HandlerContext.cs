using System.Collections.Generic;

namespace StateBased.ConsistentMessaging.Console.Infrastructure
{
    class HandlerContext : IHandlerContext
    {
        public List<object> Messages { get; set; } = new List<object>();

        public void Publish(object message)
        {
            Messages.Add(message);
        }
    }

    internal interface IHandlerContext
    {
        void Publish(object message);
    }
}