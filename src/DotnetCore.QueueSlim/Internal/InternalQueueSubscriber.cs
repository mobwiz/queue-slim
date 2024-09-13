using CloudNative.CloudEvents;
using DotnetCore.QueueSlim.Dispatchers;
using DotnetCore.QueueSlim.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace DotnetCore.QueueSlim.Internal
{
    internal class SubscriberContext
    {
        public required InternalQueueSubscriber Subscriber { get; set; }
        public required Task RunningTask { get; set; }
    }


    internal sealed class InternalQueueSubscriber : IDisposable
    {
        private const int RETRY_DELAY = 5000; // 5 秒？还是2秒？

        private IMessageDispatcher? _messageDispatcher;

        private readonly CancellationTokenSource _localCts = new CancellationTokenSource();
        private readonly IConsumerClientFactory _consumerClientFactory;
        private readonly QueueSlimOptions _eventBusOptions;
        private readonly ILogger<InternalQueueSubscriber> _logger;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="consumerClientFactory"></param>
        /// <param name="logger"></param>
        /// <param name="options"></param>
        public InternalQueueSubscriber(
            IConsumerClientFactory consumerClientFactory,
            ILogger<InternalQueueSubscriber> logger,
            IOptions<QueueSlimOptions> options
            )
        {
            _eventBusOptions = options.Value;
            _consumerClientFactory = consumerClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// 订阅消息，会阻塞，建议开启线程使用，内部会做重试，只要外面不主动取消，就会一直重试
        /// </summary>
        /// <param name="topics">订阅的主题列表</param>
        /// <param name="groupName">分组名称（队列名称）</param>
        /// <param name="onMessageReceived">消息回调，此处没有重试，如果回调异常，消息会被 reject 到队列中</param>
        /// <param name="cancellationToken">取消tokenSource</param>
        /// <param name="autoUnsubscribe">是否在取消后，自动取消订阅</param>
        /// <param name="dispatchAsync">是否异步分发，异步分发时，会自动 commit 消息，请自行做好重试逻辑。不启用异步分发，则不会自动 commit 消息，必须处理成功，不抛出异常，才会 commit 消息</param>
        /// <exception cref="Exception"></exception>
        public async Task Subscribe(IEnumerable<string> topics,
            string groupName,
            Func<CloudEvent, Task> onMessageReceived,
            CancellationToken cancellationToken,
            bool autoUnsubscribe = true,
            bool dispatchAsync = false
            )
        {
            if (topics is null)
            {
                throw new ArgumentNullException(nameof(topics));
            }

            if (string.IsNullOrEmpty(groupName))
            {
                throw new ArgumentNullException(nameof(groupName));
            }

            if (onMessageReceived is null)
            {
                throw new ArgumentNullException(nameof(onMessageReceived));
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await DoSubscribe(topics, groupName, onMessageReceived, cancellationToken, autoUnsubscribe, dispatchAsync);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Subscribe failed, will retry after {RETRY_DELAY} ms.");
                }

                await Task.Delay(RETRY_DELAY, cancellationToken);
            }
        }

        private async Task DoSubscribe(IEnumerable<string> topics,
            string groupName,
            Func<CloudEvent, Task> onMessageReceived,
            CancellationToken cancellationToken,
            bool autoUnsubscribe = true,
            bool dispatchAsync = false   // 异步分发
            )
        {

            _messageDispatcher = dispatchAsync ? new AsynchronousMessageDispatcher(_logger) : new SynchronousMessageDispatcher(_logger);
            _messageDispatcher.OnMessageCallback = onMessageReceived;

            if (dispatchAsync)
            {
                await ((AsynchronousMessageDispatcher)_messageDispatcher).StartAsync(cancellationToken);
            }

            using var consumer = _consumerClientFactory.Create(groupName, autoUnsubscribe) ?? throw new InvalidOperationException("Create ConsumerClient failed.");
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _localCts.Token);

            consumer.OnLogCallback = log =>
            {
                _logger.LogInformation($"[ConsumerClient log calblack] : {log.LogType}, {log.Reason}");

                switch (log.LogType)
                {
                    case MqLogType.ConsumerShutdown:
                    case MqLogType.ServerConnError:
                    case MqLogType.ConnectError:
                    case MqLogType.ExceptionReceived:
                        // 如果外部的 token 被取消了
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            _logger.LogWarning($"ConsumerClient log: {log.LogType}, {log.Reason}, will restart listening.");
                            linkedCts.Cancel();
                        }

                        break;
                    default:
                        break;
                }
            };

            consumer.OnMessageCallback = async (cloudEvent, sender) =>
            {
                _logger.LogTrace($"Message received：{cloudEvent.Id}, {cloudEvent.Type}");
                // 解析事件

                try
                {
                    cloudEvent.Validate();
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError("Message is invalid: {0}", ex.Message);
                }

                if (!cloudEvent.IsValid)
                {
                    // invalid cloud event in queue, log error and skip it!
                    _logger.LogError($"Message is invalid: {cloudEvent.ToString()}");
                    consumer.Commit(sender);
                    return;
                }

                if (cloudEvent.Data != null)
                {
                    try
                    {
                        // onMessageReceived?.Invoke(args, new CapHeader(transportMessage.Headers)).GetAwaiter().GetResult();
                        await _messageDispatcher.DispatchMessage(cloudEvent);
                        consumer.Commit(sender);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "OnMessageReceived failed, message will be reject to MQ");
                        consumer.Reject(sender);
                    }
                }
            };

            var topicNames = new List<string>();

            try
            {
                topicNames = consumer.FetchTopics(topics).ToList();
                consumer.Subscribe(topicNames);
                consumer.Listening(TimeSpan.FromSeconds(1), linkedCts.Token);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogTrace(ex, "Subscrption canceled");
                _logger.LogInformation("Subscrption canceled, due to cancellationTokenSource is canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Subscribe failed for other reasons");
            }

            try
            {
                if (autoUnsubscribe && cancellationToken.IsCancellationRequested)
                {
                    consumer.Unsubscribe(topicNames);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unsubscribe failed");
            }
            finally
            {
                try
                {
                    consumer?.Dispose();
                }
                catch (Exception ex2)
                {
                    _logger.LogWarning(ex2, "consumer dispose failed.");
                }
            }
        }

        public void Dispose()
        {
            _messageDispatcher?.Dispose();
        }
    }
}