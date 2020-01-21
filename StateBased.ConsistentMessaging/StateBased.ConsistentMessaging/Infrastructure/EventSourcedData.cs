using System.Collections.Generic;

namespace StateBased.ConsistentMessaging.Infrastructure
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
