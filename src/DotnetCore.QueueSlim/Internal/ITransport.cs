using CloudNative.CloudEvents;

namespace DotnetCore.QueueSlim.Internal
{
    public interface ITransport
    {
        /// <summary>
        /// Broker address 
        /// </summary>
        BrokerAddress BrokerAddress { get; }

        /// <summary>
        /// Send the cloud event to MQ
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task<OperateResult> SendAsync(CloudEvent message);
    }

    public static class CloudEventProperExtensions
    {
        public static string? GetGroup(this CloudEvent cloudEvent)
        {
            return cloudEvent[MessageConstants.Names.Group]?.ToString() ?? null;
        }
    }

}
