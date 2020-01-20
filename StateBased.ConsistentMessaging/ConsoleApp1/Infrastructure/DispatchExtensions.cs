using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Routing;
using NServiceBus.Transport;

namespace StateBased.ConsistentMessaging.Console.Infrastructure
{
    static class DispatchExtensions
    {
        public static Task Send(this IDispatchMessages endpoint, Message[] messages) =>
            Task.WhenAll(messages.Select(endpoint.Send));

        public static Task Send(this IDispatchMessages endpoint, Message message)
        {
            var headers = new Dictionary<string, string>
            {
                {"Message.Id", message.Id.ToString() }
            };

            var body = Serializer.Serialize(message, headers);

            var request = new OutgoingMessage(
                messageId: Guid.NewGuid().ToString(),
                headers: headers,
                body: body);

            var operation = new TransportOperation(
                request,
                new UnicastAddressTag(Program.EndpointName));

            return endpoint.Dispatch(
                outgoingMessages: new TransportOperations(operation),
                transaction: new TransportTransaction(),
                context: new ContextBag());
        }
    }
}