using CloudNative.CloudEvents;

namespace DotnetCore.QueueSlim.Dispatchers
{
    public interface IMessageDispatcher : IDisposable
    {
        Func<CloudEvent, Task> OnMessageCallback { get; set; }

        Task DispatchMessage(CloudEvent args);
    }
}
