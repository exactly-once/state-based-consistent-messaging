using System;
using System.Threading.Tasks;
using NServiceBus.Raw;
using NServiceBus.Transport;

namespace StateBased.ConsistentMessaging.Console.Infrastructure
{
    static class HandlerInvoker
    {
        private static SagaStore SagaStore;
        private static IReceivingRawEndpoint Endpoint;

        public static Task OnMessage(MessageContext context, SagaStore sagaStore, IReceivingRawEndpoint endpoint)
        {
            var message = Serializer.Deserialize(context.Body, context.Headers);

            SagaStore = sagaStore;
            Endpoint = endpoint;

            if (message is FireAt fireAt)
            {
                return Invoke<ShootingRange, FireAt>(fireAt.GameId, fireAt);
            }

            if (message is MoveTarget moveTarget)
            {
                return Invoke<ShootingRange, MoveTarget>(moveTarget.GameId, moveTarget);
            }

            if (message is Missed missed)
            {
                return Invoke<LeaderBoard, Missed>(missed.GameId, missed);
            }

            if (message is Hit hit)
            {
                return Invoke<LeaderBoard, Hit>(hit.GameId, hit);
            }

            System.Console.WriteLine($"Unknown message type: {message.GetType().FullName}");

            return Task.CompletedTask;
        }

        static async Task Invoke<TSaga, TMessage>(Guid sagaId, TMessage inputMessage) where TSaga : new()
        {
            var handlerContext = new HandlerContext();

            var (saga, version) = SagaStore.Get<TSaga>(sagaId);

            ((dynamic)saga).Handle(handlerContext, inputMessage);
            
            SagaStore.Save(sagaId, saga, version);

            foreach (var outgoingMessage in handlerContext.Messages)
            {
                await Endpoint.Send(outgoingMessage);
            }
        }
    }
}