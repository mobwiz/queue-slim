// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNative.CloudEvents;
using DotnetCore.QueueSlim.Internal;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Kafka
{
    public class KafkaConsumerClientFactory : IConsumerClientFactory
    {
        private readonly IOptions<KafkaOptions> _kafkaOptions;
        private readonly CloudEventFormatter _formatter;

        public KafkaConsumerClientFactory(IOptions<KafkaOptions> kafkaOptions, CloudEventFormatter formatter)
        {
            _kafkaOptions = kafkaOptions;
            _formatter = formatter;
        }

        public virtual IConsumerClient Create(string groupId, bool autoUnsubscribe)
        {
            try
            {
                return new KafkaConsumerClient(groupId, _kafkaOptions, _formatter, autoUnsubscribe);
            }
            catch (System.Exception e)
            {
                throw new BrokerConnectionException(e);
            }
        }
    }
}