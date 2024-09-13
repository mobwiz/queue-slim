using CloudNative.CloudEvents;
using Microsoft.Extensions.Logging;
using Polly;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace DotnetCore.QueueSlim.Dispatchers
{

    public class AsynchronousMessageDispatcher : IMessageDispatcher
    {
        private const int _maxRetry = 10;
        private ILogger _logger;
        private CancellationTokenSource _internalCts = new CancellationTokenSource();

        private readonly Channel<CloudEvent> _channel = Channel.CreateUnbounded<CloudEvent>();


        public AsynchronousMessageDispatcher(ILogger logger)
        {
            _logger = logger;
        }

        public Func<CloudEvent, Task> OnMessageCallback { get; set; }

        public async Task DispatchMessage(CloudEvent args)
        {
            await _channel.Writer.WriteAsync(args);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (OnMessageCallback == null)
            {
                throw new ArgumentNullException("OnMessageCallback is not set");
            }

            _logger.LogInformation("AsynchronousMessageDispatcher Starting...");

            // 启动后台处理任务，但不等待它完成
            _ = Task.Run(() => ProcessMessagesAsync(cancellationToken));
            // await InitializeAsync(cancellationToken);
            _logger.LogInformation("AsynchronousMessageDispatcher Started.");


            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _internalCts.Cancel();
            _internalCts.Dispose();
            _channel.Writer.Complete();
        }


        private async Task ProcessMessagesAsync(CancellationToken externalCancellationToken)
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken, _internalCts.Token);
            try
            {
                await foreach (var body in ReadMessagesAsync(linkedCts.Token))
                {
                    await ProcessMessageWithRetryAsync(body, linkedCts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("AsynchronousMessageDispatcher stopped due to cancellation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AsynchronousMessageDispatcher stopped due to exception");
            }
            finally
            {
                _logger.LogInformation("AsynchronousMessageDispatcher stopped.");
            }
        }

        private async IAsyncEnumerable<CloudEvent> ReadMessagesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (await _channel.Reader.WaitToReadAsync(cancellationToken))
            {
                while (_channel.Reader.TryRead(out var body))
                {
                    if (body == null)
                    {
                        _logger.LogWarning("Received a null message!");
                        continue;
                    }
                    yield return body;
                }
            }
        }

        private async Task ProcessMessageWithRetryAsync(CloudEvent body, CancellationToken cancellationToken)
        {
            _logger.LogTrace("Message received, {Type}, {Id}", body.Type ?? "-", body.Id ?? "-");

            var retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    _maxRetry,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    OnRetry);

            try
            {
                await retryPolicy.ExecuteAsync(ct => OnMessageCallback(body), cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "OnMessageCallback failed after {MaxRetry} retries. Event: {Type}, Id: {Id}", _maxRetry, body.Type, body.Id);
            }

            void OnRetry(Exception exception, TimeSpan timespan, int retryAttempt, Context context)
            {
                _logger.LogDebug("Retry attempt {RetryAttempt} for message {Id}. Reason: {ExceptionMessage}. Next retry in {Timespan}.",
                    retryAttempt, body?.Id, exception.Message, timespan);
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
    }
}
