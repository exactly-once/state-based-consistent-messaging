using System;
using System.Collections.Concurrent;

namespace StateBased.ConsistentMessaging.Console.Infrastructure
{
    class SagaStore
    {
        readonly ConcurrentDictionary<(Guid, Type), (int, object)> sagas = new ConcurrentDictionary<(Guid, Type), (int, object)>();

        public (T, int) Get<T>(Guid sagaId) where T : new()
        {
            var storageId = (sagaId, typeof(T));

            if (!sagas.TryGetValue(storageId, out var saga))
            {
                return (new T(), 0);
            }

            var (version, instance) = saga;

            return ((T) instance, version);
        }

        public void Save<T>(Guid sagaId, T saga, int version)
        {
            var storageId = (sagaId, typeof(T));

            sagas.AddOrUpdate(storageId, (version + 1, saga), (_, t) =>
            {
                if (t.Item1 == version)
                {
                    return (version + 1, saga);
                }

                throw new Exception("Optimistic concurrency error.");
            });
        }
    }
}