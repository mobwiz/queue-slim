using CloudNative.CloudEvents;

namespace DotnetCore.QueueSlim.Internal
{
    public interface IConsumerClient : IDisposable
    {
        BrokerAddress BrokerAddress { get; }

        /// <summary>
        /// Create (if necessary) and get topic identifiers
        /// </summary>
        /// <param name="topicNames">Names of the requested topics</param>
        /// <returns>Topic identifiers</returns>
        ICollection<string> FetchTopics(IEnumerable<string> topicNames)
        {
            return topicNames.ToList();
        }

        /// <summary>
        /// Subscribe to a set of topics to the message queue
        /// </summary>
        /// <param name="topics"></param>
        void Subscribe(IEnumerable<string> topics);

        /// <summary>
        /// Start listening
        /// </summary>
        void Listening(TimeSpan timeout, CancellationToken cancellationToken);

        /// <summary>
        /// Manual submit message offset when the message consumption is complete
        /// </summary>
        void Commit(object? sender);

        /// <summary>
        /// Reject message and resumption
        /// </summary>
        void Reject(object? sender);

        /// <summary>
        /// On message callback
        /// </summary>
        public Func<CloudEvent, object?, Task>? OnMessageCallback { get; set; }

        /// <summary>
        /// On log callback
        /// </summary>
        public Action<LogMessageEventArgs>? OnLogCallback { get; set; }

        /// <summary>
        /// Unsubscribe topics
        /// </summary>
        /// <param name="topics"></param>
        void Unsubscribe(IEnumerable<string> topics);
    }

}