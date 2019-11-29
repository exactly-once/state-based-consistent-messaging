using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace StateBased.ConsistentMessaging.Console.Infrastructure
{
    public static class Serializer {

        public const string MessageTypeName = "MessageType";

        public static byte[] Serialize(object message, Dictionary<string, string> headers)
        {
            headers.Add(MessageTypeName, message.GetType().FullName);

            var text = JsonSerializer.Serialize(message);

            return Encoding.UTF8.GetBytes(text);
        }

        public static object Deserialize(byte[] body, Dictionary<string, string> headers)
        {
            var messageType = headers[Serializer.MessageTypeName];
            var bodyText = Encoding.UTF8.GetString(body);
            var message = JsonSerializer.Deserialize(bodyText, Type.GetType(messageType));

            return message;
        }
    }
}