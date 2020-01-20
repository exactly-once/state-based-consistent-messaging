using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StateBased.ConsistentMessaging.Console.Infrastructure
{
    static class HandlerInvoker
    {
        private static SagaStore SagaStore;

        public static async Task<Message[]> OnMessage(Message message, SagaStore sagaStore)
        {
            SagaStore = sagaStore;

            Func<Task<Message[]>> invoke;
            
            if (message is FireAt fireAt)
            {
                invoke = () => Invoke<ShootingRange, ShootingRange.ShootingRangeData>(fireAt.GameId, fireAt);
            } 
            else if (message is MoveTarget moveTarget)
            {
                invoke = () => Invoke<ShootingRange, ShootingRange.ShootingRangeData>(moveTarget.GameId, moveTarget);
            } 
            else if (message is Missed missed)
            {
                invoke = () => Invoke<LeaderBoard, LeaderBoard.LeaderBoardData>(missed.GameId, missed);
            } 
            else if (message is Hit hit)
            {
                invoke = () => Invoke<LeaderBoard, LeaderBoard.LeaderBoardData>(hit.GameId, hit);
            }
            else
            {
                throw new Exception($"Unknown message type: {message.GetType().FullName}");
            }

            var outputMessages = await invoke();

            return outputMessages;
        }

        static async Task<Message[]> Invoke<TSaga, TSagaData>(Guid sagaId, Message inputMessage) 
            where TSaga : new() 
            where TSagaData : EventSourcedData, new()
        {
            var messageId = inputMessage.Id;

            var (saga, stream, duplicate) = await SagaStore.LoadSaga<TSagaData>(sagaId, messageId);

            var outputMessages = InvokeHandler<TSaga, TSagaData>(inputMessage, saga);

            if (duplicate == false)
            { 
                await SagaStore.UpdateSaga(stream, saga.Changes, messageId);
            }

            return outputMessages.ToArray();
        }

        private static List<Message> InvokeHandler<TSaga, TSagaData>(object inputMessage, TSagaData saga)
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