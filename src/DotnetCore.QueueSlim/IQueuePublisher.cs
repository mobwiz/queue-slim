namespace DotnetCore.QueueSlim
{
    public interface IQueuePublisher
    {
        /// <summary>
        /// Publish an event
        /// </summary>
        /// <typeparam name="T">event object type</typeparam>
        /// <param name="name">topic name</param>
        /// <param name="contentObj">event object</param>
        /// <param name="source">event source uri</param>
        /// <param name="cancellationToken">cancellationToken</param>
        /// <returns></returns>
        Task PublishAsync<T>(string name, T? contentObj, Uri? source, CancellationToken cancellationToken = default);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T">event object type</typeparam>
        /// <param name="name">topic name</param>
        /// <param name="contentObj">event object</param>
        /// <param name="source">event source</param>
        /// <param name="headers">event headers</param>
        /// <param name="cancellationToken">cancellationToken</param>
        /// <returns></returns>
        Task PublishAsync<T>(string name, T? contentObj, Uri? source, IDictionary<string, string> headers, CancellationToken cancellationToken = default);
    }

}
