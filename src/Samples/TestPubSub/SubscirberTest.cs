// Copyright (c) .NET Core Community. All rights reserved. 
//  Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNative.CloudEvents;
using DotnetCore.QueueSlim;
using System.Text.Json;

namespace TestPubSub
{
    public class SubscirberTest : IQueueSubscriber
    {
        private ILogger<SubscirberTest> _logger;

        public SubscirberTest(ILogger<SubscirberTest> logger)
        {
            _logger = logger;
        }

        public IEnumerable<string> Topics => new List<string>
            {
                "plat.#"
            };

        public string Group => "test-queue";

        public bool IsAsync => false;

        public bool AutoUnsubscribe => true;

        public Task OnEventCallback(CloudEvent message)
        {
            // Console.WriteLine($"message received: {message.Type}, {message.Id}, {message.Time}");
            // Console.WriteLine($"message data: {JsonSerializer.Serialize(message.Data)}");

            var random = new Random();
            //if (random.Next(5) == 3)
            //{
            //    throw new Exception("test exception");
            //}
            _logger.LogInformation($">>>>>>>>>>>>>>>>>> message processed: {message.Type}, {message.Id}");

            return Task.CompletedTask;
        }
    }
}
