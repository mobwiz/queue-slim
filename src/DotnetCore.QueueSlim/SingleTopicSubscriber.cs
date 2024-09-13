// Copyright (c) .NET Core Community. All rights reserved. 
//  Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNative.CloudEvents;
using Microsoft.Extensions.Logging;

namespace DotnetCore.QueueSlim
{
    public abstract class SingleTopicSubscriber : IQueueSubscriber
    {
        protected ILogger _logger;
        public abstract string Topic { get; }
        public IEnumerable<string> Topics => new List<string> { Topic };

        public abstract string Group { get; }

        public abstract bool IsAsync { get; }

        public abstract bool AutoUnsubscribe { get; }

        public Task OnEventCallback(CloudEvent message)
        {
            if (message.Type == Topic)
            {
                return HandleEventCallback(message);
            }

            return Task.CompletedTask;
        }

        protected abstract Task HandleEventCallback(CloudEvent message);        

        protected SingleTopicSubscriber(ILogger logger)
        {
            _logger = logger;
        }
    }
}
