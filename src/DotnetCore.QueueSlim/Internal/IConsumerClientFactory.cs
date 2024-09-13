namespace DotnetCore.QueueSlim.Internal
{
    public interface IConsumerClientFactory
    {
        /// <summary>
        /// create a IConsumerClient
        /// </summary>
        /// <param name="groupId">group id, used as group id for kafka, queue name for rabbitmq, such....</param>
        /// <returns></returns>
        IConsumerClient Create(string groupId, bool autoUnsubscribe);
    }

}
