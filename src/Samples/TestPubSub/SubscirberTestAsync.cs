// Copyright (c) .NET Core Community. All rights reserved. 
//  Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNative.CloudEvents;
using DotnetCore.QueueSlim;
using System.Text.Json;

namespace TestPubSub
{
    public class SingleSubscirberTest : SingleTopicSubscriber
    {
        public SingleSubscirberTest(ILogger<SingleSubscirberTest> logger)
            : base(logger)
        {
            _logger = logger;
        }

        public override string Topic => "plat.test5";

        public override string Group => "test-single-queue";

        public override bool IsAsync => false;

        public override bool AutoUnsubscribe => false;

        protected override Task HandleEventCallback(CloudEvent message)
        {
            Console.WriteLine($"message received: {message.Type}, {message.Id}, {message.Time}");
            return Task.CompletedTask;
        }
    }

    public class MultipleTest : MultipleTopicQueueSubscriber
    {
        public override string Group => "test-multiple-queue";

        public override bool IsAsync => false;

        public override bool AutoUnsubscribe => false;

        public MultipleTest(ILogger<MultipleTest> logger)
            : base(logger)
        {
            AddEventHandler("plat.test1", async (message) =>
            {
                Console.WriteLine($"111111111111111111 message received: {message.Type}, {message.Id}, {message.Time}");
                Console.WriteLine($"message data: {JsonSerializer.Serialize(message.Data)}");
            });

            AddEventHandler("plat.test2", HandleEventCallback);

            AddEventHandler("plat.test3", async (message) =>
            {
                Console.WriteLine($"3333333333333333333333333 message received: {message.Type}, {message.Id}, {message.Time}");
                Console.WriteLine($"message data: {JsonSerializer.Serialize(message.Data)}");
            });
        }

        private async Task HandleEventCallback(CloudEvent message)
        {
            Console.WriteLine($"message received: {message.Type}, {message.Id}, {message.Time}");
            Console.WriteLine($"message data: {JsonSerializer.Serialize(message.Data)}");
        }
    }


    public class SubscirberTestAsync : IQueueSubscriber
    {
        private ILogger<SubscirberTestAsync> _logger;

        public SubscirberTestAsync(ILogger<SubscirberTestAsync> logger)
        {
            _logger = logger;
        }

        public IEnumerable<string> Topics => new List<string>
            {
                "plat.#"
            };

        public string Group => "test-queue-2";

        public bool IsAsync => true;

        public bool AutoUnsubscribe => false;

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
