using CloudNative.CloudEvents;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace DotnetCore.QueueSlim.Dispatchers
{
    public class SynchronousMessageDispatcher : IMessageDispatcher
    {
        private ILogger _logger;

        public SynchronousMessageDispatcher(ILogger logger)
        {
            _logger = logger;
        }

        public Func<CloudEvent, Task> OnMessageCallback { get; set; } = null!;

        public async Task DispatchMessage(CloudEvent args)
        {
            if (OnMessageCallback == null)
            {
                _logger.LogWarning("OnMessageCallback is not set");
                return;
            }

            var stopWatch = new Stopwatch();

            _logger.LogTrace($"Start to processMessage");
            stopWatch.Start();
            await OnMessageCallback(args);
            stopWatch.Stop();
            _logger.LogTrace($"Message processed in {stopWatch.ElapsedMilliseconds} ms");
        }

        public void Dispose()
        {
            // do nothing...
        }
    }
}
