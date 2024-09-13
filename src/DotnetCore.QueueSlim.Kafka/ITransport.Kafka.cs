// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Kafka;
using Confluent.Kafka;
using DotnetCore.QueueSlim.Internal;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Kafka
{
    internal class KafkaTransport : ITransport
    {
        private readonly IConnectionPool _connectionPool;
        private readonly ILogger _logger;
        private readonly CloudEventFormatter _formatter;

        public KafkaTransport(ILogger<KafkaTransport> logger,
            IConnectionPool connectionPool,
            CloudEventFormatter formatter)
        {
            _logger = logger;
            _connectionPool = connectionPool;
            _formatter = formatter;
        }

        public BrokerAddress BrokerAddress => new BrokerAddress("Kafka", _connectionPool.ServersAddress);

        public async Task<OperateResult> SendAsync(CloudEvent message)
        {
            var producer = _connectionPool.RentProducer();

            try
            {
                var kafkaMessage = message.ToKafkaMessage(ContentMode.Binary, _formatter);

                //var headers = new Confluent.Kafka.Headers();

                //foreach (var header in message.Headers)
                //{
                //    headers.Add(header.Value != null
                //        ? new Header(header.Key, Encoding.UTF8.GetBytes(header.Value))
                //        : new Header(header.Key, null));
                //}

                //var result = await producer.ProduceAsync(message.GetName(), new Message<string, byte[]>
                //{
                //    Headers = headers,
                //    Key = message.Headers.TryGetValue(KafkaHeaders.KafkaKey, out string? kafkaMessageKey) && !string.IsNullOrEmpty(kafkaMessageKey) ? kafkaMessageKey : message.GetId(),
                //    Value = message.Body.ToArray()!
                //});

                var result = await producer.ProduceAsync(message.Type, kafkaMessage!);

                if (result.Status == PersistenceStatus.Persisted || result.Status == PersistenceStatus.PossiblyPersisted)
                {
                    _logger.LogDebug($"kafka topic message [{message.Type}] has been published.");

                    return OperateResult.Success;
                }

                throw new PublisherSentFailedException("kafka message persisted failed!");
            }
            catch (Exception ex)
            {
                var wapperEx = new PublisherSentFailedException(ex.Message, ex);

                return OperateResult.Failed(wapperEx);
            }
            finally
            {
                _connectionPool.Return(producer);
            }
        }
    }
}