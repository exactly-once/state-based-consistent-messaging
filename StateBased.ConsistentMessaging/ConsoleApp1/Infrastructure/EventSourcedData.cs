using System;
using System.Collections.Generic;
using System.Text;

namespace StateBased.ConsistentMessaging.Console.Infrastructure
{
    abstract class EventSourcedData
    {
        public List<object> Changes = new List<object>();

        public void Apply(object @event)
        {
            ((dynamic)this).When((dynamic)@event);
                
            Changes.Add(@event);
        }
    }
}
