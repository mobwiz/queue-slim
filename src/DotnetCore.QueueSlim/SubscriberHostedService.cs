// Copyright (c) .NET Core Community. All rights reserved. 
//  Licensed under the MIT License. See License.txt in the project root for license information.

using DotnetCore.QueueSlim.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace DotnetCore.QueueSlim
{
    public class BaseSubscriberService : IHostedService, IDisposable
    {
        private ILogger<BaseSubscriberService> _logger;
        private readonly IEnumerable<IQueueSubscriber> _subscribers;
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<string, SubscriberContext> _subscriberContextList = new ConcurrentDictionary<string, SubscriberContext>();
        private CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public BaseSubscriberService(
            IServiceProvider serviceProvider,
            ILogger<BaseSubscriberService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _subscribers = _serviceProvider.GetServices<IQueueSubscriber>();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var marker = _serviceProvider.GetService<QueueMarkerService>();
            if (marker == null)
            {
                throw new ArgumentNullException("There is not message queue service registered");
            }

            if (!_subscribers.Any())
            {
                _logger.LogWarning("There is not IQueueSubscriber service registered");
                return Task.CompletedTask;
            }

            foreach (var handler in _subscribers)
            {
                InternalQueueSubscriber eventSubscriber = _serviceProvider.GetRequiredService<InternalQueueSubscriber>();

                var task = Task.Factory.StartNew(async () =>
                {
                    _logger.LogInformation($"Start to Subscribe for {handler.GetType().FullName}");
                    await eventSubscriber.Subscribe(handler.Topics, handler.Group, handler.OnEventCallback, _tokenSource.Token, handler.AutoUnsubscribe, handler.IsAsync);
                }, cancellationToken);

                _subscriberContextList.TryAdd(handler.Group, new SubscriberContext
                {
                    RunningTask = task,
                    Subscriber = eventSubscriber
                });
            }

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping BaseSubscriberService");
            _tokenSource.Cancel();
            await Task.WhenAll(_subscriberContextList.Values.Select(p => p.RunningTask).ToArray());
            _subscriberContextList.Clear();

        }

        public void Dispose()
        {
            _tokenSource?.Dispose();
        }
    }
}
