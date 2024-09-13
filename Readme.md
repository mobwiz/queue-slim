# Summary

A simple and easy-to-use message queue wrapper

# Features
- Use CloudEvents specification to transmit events
- Support multiple message queues
    - RabbitMQ
    - Kafka
    - ...
- Support automatic unsubscription
- Support asynchronous callbacks

# Design Specification
- Use CloudEvent format for transmission


# 设计规约
- 传输过程使用 CloudEvent 格式

# Usage Example
1. Register Component

```c#

 builder.Services.AddQueueSlim(options =>
 {
     options.UseRabbitMQ(options =>
     {
         options.Port = 5672;
         options.HostName = "192.168.0.111";
         options.UserName = "admin";
         options.Password = "123456";
         options.VirtualHost = "queueslim";
     });
 });

 ```


2. Publish Event
```c#
    public class PublisherService
    {
        public IQueuePublisher _eventPublisher;

        public PublisherTestBackgroundService(IQueuePublisher eventPublisher)
        {
            _eventPublisher = eventPublisher;
        }
    }
```

3. Subscribe to Event: Single Topic, Single Group Subscription

```c#

public class SingleSubscriberTest : SingleTopicSubscriber
{
    public SingleSubscriberTest(ILogger<SingleSubscriberTest> logger)
        : base(logger)
    {
        _logger = logger;
    }

    public override string Topic => "event.test5";

    public override string Group => "test-single-queue";

    public override bool IsAsync => false;

    public override bool AutoUnsubscribe => false;

    protected override Task HandleEventCallback(CloudEvent message)
    {
        Console.WriteLine($"message received: {message.Type}, {message.Id}, {message.Time}");
        return Task.CompletedTask;
    }
}
```

3.2 Multiple Topic Single Group Subscription
```c#
public class MultipleTest : MultipleTopicQueueSubscriber
{
    public override string Group => "test-multiple-queue";

    public override bool IsAsync => false;

    public override bool AutoUnsubscribe => false;

    public MultipleTest(ILogger<MultipleTest> logger)
        : base(logger)
    {
        AddEventHandler("event.test1", async (message) =>
        {
            Console.WriteLine($"111111111111111111 message received: {message.Type}, {message.Id}, {message.Time}");
            Console.WriteLine($"message data: {JsonSerializer.Serialize(message.Data)}");
        });

        AddEventHandler("event.test2", HandleEventCallback);

        AddEventHandler("event.test3", async (message) =>
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

```

3.3  Custom Subscription, Implement IQueueSubscriber
```c#
public class SubscriberTestAsync : IQueueSubscriber
{
    private ILogger<SubscriberTestAsync> _logger;

    public SubscriberTestAsync(ILogger<SubscriberTestAsync> logger)
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
        _logger.LogInformation($">>>>>>>>>>>>>>>>>> message processed: {message.Type}, {message.Id}");
        return Task.CompletedTask;
    }
}
```


