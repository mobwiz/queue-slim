// Copyright (c) .NET Core Community. All rights reserved. 
//  Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNative.CloudEvents;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;

namespace DotnetCore.QueueSlim
{
    public abstract class MultipleTopicQueueSubscriber : IQueueSubscriber
    {
        protected ILogger _logger;

        protected MultipleTopicQueueSubscriber(ILogger logger)
        {
            _logger = logger;
        }

        private ConcurrentDictionary<string, Func<CloudEvent, Task>> _eventHandlers = new ConcurrentDictionary<string, Func<CloudEvent, Task>>();

        public IEnumerable<string> Topics => _eventHandlers.Keys;
        public abstract string Group { get; }

        public abstract bool IsAsync { get; }

        public abstract bool AutoUnsubscribe { get; }

        public async Task OnEventCallback(CloudEvent message)
        {
            var type = message.Type;
            if (string.IsNullOrWhiteSpace(type))
            {
                _logger.LogWarning($"No type found for the message {message}");
                throw new Exception("Event type is required");
            }

            var key = type.ToLower();
            if (_eventHandlers.TryGetValue(key, out var handler))
            {
                await handler(message);
            }
            else
            {
                _logger.LogWarning($"No handler found for event type {type}");
            }
        }

        protected void AddEventHandler([Required] string topic, [Required] Func<CloudEvent, Task> handler)
        {
            var key = topic.ToLower();
            _eventHandlers.AddOrUpdate(key, handler, (k, v) => handler);
        }
    }
}
