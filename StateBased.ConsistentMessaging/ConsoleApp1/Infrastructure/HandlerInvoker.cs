﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StateBased.ConsistentMessaging.Console.Infrastructure
{
    class HandlerInvoker
    {
        private readonly SagaStore sagaStore;

        public HandlerInvoker(SagaStore sagaStore)
        {
            this.sagaStore = sagaStore;
        }

        public async Task<Message[]> Process(Message message)
        {
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

        async Task<Message[]> Invoke<TSaga, TSagaData>(Guid sagaId, Message inputMessage) 
            where TSaga : new() 
            where TSagaData : EventSourcedData, new()
        {
            var messageId = inputMessage.Id;

            var (saga, stream, duplicate) = await sagaStore.LoadSaga<TSagaData>(sagaId, messageId);

            var outputMessages = InvokeHandler<TSaga, TSagaData>(inputMessage, saga);

            if (duplicate == false)
            { 
                await sagaStore.UpdateSaga(stream, saga.Changes, messageId);
            }

            return outputMessages.ToArray();
        }

        static List<Message> InvokeHandler<TSaga, TSagaData>(object inputMessage, TSagaData saga)
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