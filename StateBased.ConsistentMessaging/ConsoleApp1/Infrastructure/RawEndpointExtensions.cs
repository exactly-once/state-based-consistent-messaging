using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Raw;
using NServiceBus.Routing;
using NServiceBus.Transport;

namespace StateBased.ConsistentMessaging.Console.Infrastructure
{
    static class RawEndpointExtensions
    {
        public static Task Send(this IReceivingRawEndpoint endpoint, object message)
        {
            return Send(endpoint, message, Guid.NewGuid());
        }

        public static Task Send(this IReceivingRawEndpoint endpoint, object message, Guid messageId)
        {
            var headers = new Dictionary<string, string>();
            var body = Serializer.Serialize(message, headers);

            var request = new OutgoingMessage(
                messageId: messageId.ToString(),
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