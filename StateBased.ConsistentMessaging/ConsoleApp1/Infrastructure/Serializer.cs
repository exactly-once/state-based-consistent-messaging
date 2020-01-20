using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace StateBased.ConsistentMessaging.Console.Infrastructure
{
    public static class Serializer {

        public const string MessageTypeName = "MessageType";

        internal static byte[] Serialize(object message, Dictionary<string, string> headers)
        {
            headers.Add(MessageTypeName, message.GetType().FullName);

            var text = JsonSerializer.Serialize(message);

            return Encoding.UTF8.GetBytes(text);
        }

        internal static Message Deserialize(byte[] body, Dictionary<string, string> headers)
        {
            var messageType = headers[Serializer.MessageTypeName];
            var bodyText = Encoding.UTF8.GetString(body);
            var message = (Message) JsonSerializer.Deserialize(bodyText, Type.GetType(messageType));

            message.Id = Guid.Parse(headers["Message.Id"]);

            return message;
        }
    }
}