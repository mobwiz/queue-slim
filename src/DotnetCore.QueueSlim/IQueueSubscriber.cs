// Copyright (c) .NET Core Community. All rights reserved. 
//  Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNative.CloudEvents;

namespace DotnetCore.QueueSlim
{
    public interface IQueueSubscriber
    {
        /// <summary>
        /// Callback method for event bus messages
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task OnEventCallback(CloudEvent message);

        /// <summary>
        /// Topics to subscribe to
        /// </summary>
        IEnumerable<string> Topics { get; }

        /// <summary>
        /// Subscription group id, which is used to ensure that the same group of messages is only consumed by one subscriber
        /// group will be used as the queue name in rabbitmq
        /// group will be used as the consumer group id in kafka
        /// </summary>
        string Group { get; }

        /// <summary>
        /// Callback async or not.
        /// If async is true, the callback method will be executed asynchronously, and retry on exception for 5 times, then log an error and continue to the next message   
        /// If async is false, the callback method will be executed synchronously, and will not retry on exception, the message will be rejected to the queue
        /// </summary>
        bool IsAsync { get; }

        /// <summary>
        /// Auto unsubscribe after application shutdown
        /// If autoUnsubscribe is true, the subscriber will automatically unsubscribe after the application is shut down,
        /// Else, the subscriber will leave the queue as durable in rabbitmq.
        /// </summary>
        bool AutoUnsubscribe { get; }
    }
}
