// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotnetCore.QueueSlim.Internal;
using DotnetCore.QueueSlim.Options;
using DotNetCore.CAP;
using DotNetCore.CAP.Kafka;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace DotnetCore.QueueCoreSlim.Kafka
{
    internal sealed class KafkaCapOptionsExtension : IQueueOptionsExtension
    {
        private readonly Action<KafkaOptions> _configure;

        public KafkaCapOptionsExtension(Action<KafkaOptions> configure)
        {
            _configure = configure;
        }

        public void AddServices(IServiceCollection services)
        {
            services.AddSingleton(new QueueMarkerService("Kafka"));

            services.Configure(_configure);

            services.AddSingleton<ITransport, KafkaTransport>();
            services.AddSingleton<IConsumerClientFactory, KafkaConsumerClientFactory>();
            services.AddSingleton<IConnectionPool, ConnectionPool>();
        }
    }
}