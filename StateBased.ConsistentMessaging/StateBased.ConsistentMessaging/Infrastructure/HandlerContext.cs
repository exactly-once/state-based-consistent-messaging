﻿using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using StateBased.ConsistentMessaging.Domain;

namespace StateBased.ConsistentMessaging.Infrastructure
{
    class HandlerContext : IHandlerContext
    {
        Guid seed;
        
        int guidIndex;
        
        public Random Random { get; }

        public List<Message> Messages { get; set; } = new List<Message>();

        public HandlerContext(Guid seed)
        {
            this.seed = seed;

            Random = new Random(seed.ToString().GetHashCode());
        }

        public void Publish(Message message)
        {
            Messages.Add(message);
        }

        public Guid NewGuid()
        {
            var seedBytes = Encoding.UTF8.GetBytes($"{seed}-{guidIndex++}");
            var seedHash = new SHA1CryptoServiceProvider().ComputeHash(seedBytes);
            
            Array.Resize(ref seedHash, 16);
            
            return new Guid(seedHash);
        }
    }

    internal interface IHandlerContext
    {
        void Publish(Message message);

        Guid NewGuid();

        Random Random { get; }
    }
}