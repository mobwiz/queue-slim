// Copyright (c) .NET Core Community. All rights reserved. 
//  Licensed under the MIT License. See License.txt in the project root for license information.

using DotnetCore.QueueSlim.Internal;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotnetCore.QueueSlim
{
    public class QueueSlimHealthCheck : IHealthCheck
    {
        private readonly IConsumerClientFactory _consumerClientFactory;
        private readonly ILogger<QueueSlimHealthCheck> _logger;

        public QueueSlimHealthCheck(IConsumerClientFactory consumerClientFactory, ILogger<QueueSlimHealthCheck> logger)
        {
            _consumerClientFactory = consumerClientFactory;
            _logger = logger;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = _consumerClientFactory.Create("queueslim.healthcheck", true);
                client.FetchTopics(new string[] { "queueslim.healthcheck" });

                return Task.FromResult(HealthCheckResult.Healthy());
            }
            catch (Exception ex)
            {
                _logger.LogWarning("QueueSlimHealthCheck failed: {0}", ex.Message);
                return Task.FromResult(HealthCheckResult.Unhealthy());
            }
        }
    }
}
