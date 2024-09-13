// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using DotnetCore.QueueSlim.Options;
using DotnetCore.QueueSlim.Rabbitmq;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class QueueSlimOptionsExtensions
    {
        public static QueueSlimOptions UseRabbitMQ(this QueueSlimOptions options, string hostName)
        {
            return options.UseRabbitMQ(opt => { opt.HostName = hostName; });
        }

        public static QueueSlimOptions UseRabbitMQ(this QueueSlimOptions options, Action<RabbitMQOptions> configure)
        {
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            options.RegisterExtension(new RabbitMQCapOptionsExtension(configure));

            return options;
        }
    }
}