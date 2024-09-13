// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNative.CloudEvents;
using CloudNative.CloudEvents.RabbitMQ;
using DotnetCore.QueueSlim.Internal;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace DotnetCore.QueueSlim.Rabbitmq
{
    internal sealed class RabbitMQTransport : ITransport
    {
        private readonly IConnectionChannelPool _connectionChannelPool;
        private readonly string _exchange;
        private readonly ILogger _logger;
        private readonly CloudEventFormatter _formatter;

        public RabbitMQTransport(
            ILogger<RabbitMQTransport> logger,
            IConnectionChannelPool connectionChannelPool,
            CloudEventFormatter formatter)
        {
            _logger = logger;
            _connectionChannelPool = connectionChannelPool;
            _exchange = _connectionChannelPool.Exchange;
            _formatter = formatter;
        }

        public BrokerAddress BrokerAddress => new("RabbitMQ", _connectionChannelPool.HostAddress);

        public Task<OperateResult> SendAsync(CloudEvent message)
        {
            IModel? channel = null;
            try
            {
                channel = _connectionChannelPool.Rent();

                var rabbitMessage = message.ToRabbitMQMessage(ContentMode.Structured, _formatter);

                var props = channel.CreateBasicProperties();
                props.DeliveryMode = 2;
                props.Headers = rabbitMessage.Headers.ToDictionary(x => x.Key, x => x.Value);

                channel.BasicPublish(_exchange, message.Type, props, rabbitMessage.Body);

                // Enable publish confirms
                if (channel.NextPublishSeqNo > 0) channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));

                _logger.LogInformation("CAP message '{0}' published, internal id '{1}'", message.Type, message.Id);

                return Task.FromResult(OperateResult.Success);
            }
            catch (Exception ex)
            {
                var wrapperEx = new PublisherSentFailedException(ex.Message, ex);
                var errors = new OperateError
                {
                    Code = ex.HResult.ToString(),
                    Description = ex.Message
                };

                return Task.FromResult(OperateResult.Failed(wrapperEx, errors));
            }
            finally
            {
                if (channel != null) _connectionChannelPool.Return(channel);
            }
        }
    }
}