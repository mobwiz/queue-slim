// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotnetCore.QueueCoreSlim.Kafka;
using DotnetCore.QueueSlim.Options;
using DotNetCore.CAP;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class CapOptionsExtensions
    {
        /// <summary>
        /// Configuration to use kafka in CAP.
        /// </summary>
        /// <param name="options">CAP configuration options</param>
        /// <param name="bootstrapServers">Kafka bootstrap server urls.</param>
        public static QueueSlimOptions UseKafka(this QueueSlimOptions options, string bootstrapServers)
        {
            return options.UseKafka(opt => { opt.Servers = bootstrapServers; });
        }

        /// <summary>
        /// Configuration to use kafka in CAP.
        /// </summary>
        /// <param name="options">CAP configuration options</param>
        /// <param name="configure">Provides programmatic configuration for the kafka .</param>
        /// <returns></returns>
        public static QueueSlimOptions UseKafka(this QueueSlimOptions options, Action<KafkaOptions> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            options.RegisterExtension(new KafkaCapOptionsExtension(configure));

            return options;
        }
    }
}