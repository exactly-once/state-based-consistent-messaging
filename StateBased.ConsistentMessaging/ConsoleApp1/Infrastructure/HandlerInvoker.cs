using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Raw;
using NServiceBus.Transport;

namespace StateBased.ConsistentMessaging.Console.Infrastructure
{
    static class HandlerInvoker
    {
        private static IReceivingRawEndpoint Endpoint;
        private static SagaStore SagaStore;

        public static Task<Guid> OnMessage(MessageContext context, SagaStore sagaStore, IReceivingRawEndpoint endpoint)
        {
            var message = Serializer.Deserialize(context.Body, context.Headers);

            SagaStore = sagaStore;
            Endpoint = endpoint;

            if (message is FireAt fireAt)
            {
                return Invoke<ShootingRange, ShootingRange.ShootingRangeData>(fireAt.GameId, fireAt);
            }

            if (message is MoveTarget moveTarget)
            {
                return Invoke<ShootingRange, ShootingRange.ShootingRangeData>(moveTarget.GameId, moveTarget);
            }

            if (message is Missed missed)
            {
                return Invoke<LeaderBoard, LeaderBoard.LeaderBoardData>(missed.GameId, missed);
            }

            if (message is Hit hit)
            {
                return Invoke<LeaderBoard, LeaderBoard.LeaderBoardData>(hit.GameId, hit);
            }

            System.Console.WriteLine($"Unknown message type: {message.GetType().FullName}");

            throw new Exception("Unknown message type");
        }

        static async Task<Guid> Invoke<TSaga, TSagaData>(Guid sagaId, Message inputMessage) 
            where TSaga : new() 
            where TSagaData : EventSourcedData, new()
        {
            var messageId = inputMessage.LogicalId;

            var (saga, stream, duplicate) = await SagaStore.LoadSaga<TSagaData>(sagaId, messageId);

            var outputMessages = InvokeHandler<TSaga, TSagaData>(inputMessage, saga);

            if (duplicate == false)
            { 
                await SagaStore.UpdateSaga(stream, saga.Changes, messageId);
            }

            await Task.WhenAll(outputMessages.Select(m => Endpoint.Send(m)));

            return messageId;
        }

        private static List<object> InvokeHandler<TSaga, TSagaData>(object inputMessage, TSagaData saga)
            where TSaga : new() where TSagaData : EventSourcedData, new()
        {
            var handler = new TSaga();
            var handlerContext = new HandlerContext();

            ((dynamic) handler).Data = saga;
            ((dynamic) handler).Handle(handlerContext, (dynamic) inputMessage);
            
            return handlerContext.Messages;
        }
    }
}