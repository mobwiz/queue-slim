// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotnetCore.QueueSlim.Internal;
using DotnetCore.QueueSlim.Options;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace DotnetCore.QueueSlim.Rabbitmq
{
    internal sealed class RabbitMQCapOptionsExtension : IQueueOptionsExtension
    {
        private readonly Action<RabbitMQOptions> _configure;

        public RabbitMQCapOptionsExtension(Action<RabbitMQOptions> configure)
        {
            _configure = configure;
        }

        public void AddServices(IServiceCollection services)
        {
            services.AddSingleton(new QueueMarkerService("RabbitMQ"));

            services.Configure(_configure);
            services.AddSingleton<ITransport, RabbitMQTransport>();
            services.AddSingleton<IConsumerClientFactory, RabbitMQConsumerClientFactory>();
            services.AddSingleton<IConnectionChannelPool, ConnectionChannelPool>();
        }
    }
}