// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.


using DotnetCore.QueueSlim.Internal;
using Microsoft.Extensions.Options;

namespace DotnetCore.QueueSlim.Rabbitmq
{
    internal sealed class RabbitMQConsumerClientFactory : IConsumerClientFactory
    {
        private readonly IConnectionChannelPool _connectionChannelPool;
        private readonly IOptions<RabbitMQOptions> _rabbitMQOptions;
        private readonly IServiceProvider _serviceProvider;

        public RabbitMQConsumerClientFactory()
        {
        }

        public RabbitMQConsumerClientFactory(IOptions<RabbitMQOptions> rabbitMQOptions, IConnectionChannelPool channelPool,
            IServiceProvider serviceProvider)
        {
            _rabbitMQOptions = rabbitMQOptions;
            _connectionChannelPool = channelPool;
            _serviceProvider = serviceProvider;
        }

        public IConsumerClient Create(string groupId, bool autoUnsubscribe)
        {
            try
            {
                var client = new RabbitMQConsumerClient(groupId, 1, _connectionChannelPool,
                    _rabbitMQOptions, _serviceProvider, autoUnsubscribe);

                client.Connect();

                return client;
            }
            catch (Exception e)
            {
                throw new BrokerConnectionException(e);
            }
        }
    }
}