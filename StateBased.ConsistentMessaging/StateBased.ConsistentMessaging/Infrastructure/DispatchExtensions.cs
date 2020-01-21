using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Routing;
using NServiceBus.Transport;
using StateBased.ConsistentMessaging.Domain;

namespace StateBased.ConsistentMessaging.Infrastructure
{
    static class DispatchExtensions
    {
        public static Task Send(this IDispatchMessages endpoint, Message[] messages, Guid? runId = null) 
            => Task.WhenAll(messages.Select(m => endpoint.Send(m, runId)));

        public static Task Send(this IDispatchMessages endpoint, Message message, Guid? runId = null)
        {
            runId ??= Guid.NewGuid();

            var headers = new Dictionary<string, string>
            {
                {"Message.Id", message.Id.ToString() },
                {"Message.RunId", runId.ToString() }
            };

            var body = Serializer.Serialize(message, headers);

            var request = new OutgoingMessage(
                messageId: Guid.NewGuid().ToString(),
                headers: headers,
                body: body);

            var operation = new TransportOperation(
                request,
                new UnicastAddressTag(EndpointBuilder.EndpointName));

            return endpoint.Dispatch(
                outgoingMessages: new TransportOperations(operation),
                transaction: new TransportTransaction(),
                context: new ContextBag());
        }
    }
}