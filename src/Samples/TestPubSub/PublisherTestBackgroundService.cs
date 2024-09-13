// Copyright (c) .NET Core Community. All rights reserved. 
//  Licensed under the MIT License. See License.txt in the project root for license information.

using DotnetCore.QueueSlim;

namespace TestPubSub
{
    public class PublisherTestBackgroundService : BackgroundService
    {
        public IQueuePublisher _eventPublisher;

        public PublisherTestBackgroundService(IQueuePublisher eventPublisher)
        {
            _eventPublisher = eventPublisher;
        }

        public async Task Publish()
        {
            await _eventPublisher.PublishAsync("plat.test1", new { Name = "test" }, new Uri("http://localhost:5000"));
            await Task.Delay(200);
            await _eventPublisher.PublishAsync("plat.test2", new { Name = "test" }, new Uri("http://localhost:5000"));
            await Task.Delay(200);
            await _eventPublisher.PublishAsync("plat.test3", new { Name = "test" }, new Uri("http://localhost:5000"));
            await Task.Delay(200);
            await _eventPublisher.PublishAsync("plat.test4", new { Name = "test" }, new Uri("http://localhost:5000"));
            await Task.Delay(200);
            await _eventPublisher.PublishAsync("plat.test5", new { Name = "test" }, new Uri("http://localhost:5000"));
            await Task.Delay(200);
            await _eventPublisher.PublishAsync("plat.test6", new { Name = "test" }, new Uri("http://localhost:5000"));
            await Task.Delay(200);
            await _eventPublisher.PublishAsync("plat.test7", new { Name = "test" }, new Uri("http://localhost:5000"));
            await Task.Delay(200);
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(3000);
            Console.WriteLine("publish message");

            while (!stoppingToken.IsCancellationRequested)
            {
                await Publish();
                await Task.Delay(1000);
            }
        }
    }
}
