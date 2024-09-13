// Copyright (c) .NET Core Community. All rights reserved. 
//  Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNative.CloudEvents;

namespace DotnetCore.QueueSlim.Internal
{
    internal class DefaultEventPublisher : IQueuePublisher
    {
        private readonly ITransport _transport;

        public DefaultEventPublisher(ITransport transport)
        {
            _transport = transport;
        }

        public async Task PublishAsync<T>(string name, T? contentObj, Uri? source, CancellationToken cancellationToken = default)
        {
            await PublishInternalAsync(name, contentObj, source, new Dictionary<string, string>(), cancellationToken);
        }

        public async Task PublishAsync<T>(string name, T? contentObj, Uri? source, IDictionary<string, string> headers, CancellationToken cancellationToken = default)
        {
            await PublishInternalAsync(name, contentObj, source, headers, cancellationToken);
        }

        private async Task PublishInternalAsync<T>(string name, T? value, Uri? source, IDictionary<string, string> headers, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            var messageId = Guid.NewGuid().ToString("N");

            var message = new CloudEvent
            {
                Id = messageId,
                Type = name,
                Data = value,
                DataContentType = "application/json",
                Time = DateTime.UtcNow,
                Source = source,
            };

            if (headers?.Any() == true)
            {
                foreach (var item in headers)
                {
                    if (item.Value != null)
                    {
                        message.SetAttributeFromString(item.Key, item.Value ?? string.Empty);
                    }
                }
            }

            if (!message.IsValid)
            {
                throw new Exception("Message [CloudEvent] is not valid");
            }

            try
            {
                // trace
                await _transport.SendAsync(message);
            }
            catch (Exception e)
            {
                // log error
                throw;
            }
        }
    }
}
