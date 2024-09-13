namespace DotnetCore.QueueSlim.Internal
{
    public enum MqLogType
    {
        //RabbitMQ
        ConsumerCancelled,
        ConsumerRegistered,
        ConsumerUnregistered,
        ConsumerShutdown,

        //Kafka
        ConsumeError,
        ConsumeRetries,
        ServerConnError,

        //AzureServiceBus
        ExceptionReceived,

        //NATS
        AsyncErrorEvent,
        ConnectError,

        //Amazon SQS
        InvalidIdFormat,
        MessageNotInflight
    }

    public class LogMessageEventArgs : EventArgs
    {
        public string? Reason { get; set; }

        public MqLogType LogType { get; set; }
    }






    public class BrokerConnectionException : Exception
    {
        public BrokerConnectionException(Exception innerException)
            : base("Broker Unreachable", innerException)
        {
        }
    }

    public class PublisherSentFailedException : Exception
    {
        public PublisherSentFailedException(string message) : base(message)
        {
        }

        public PublisherSentFailedException(string message, Exception? ex) : base(message, ex)
        {
        }
    }

    public struct BrokerAddress
    {
        public BrokerAddress(string address)
        {
            if (address.Contains("$"))
            {
                var parts = address.Split('$');

                Name = parts[0];
                Endpoint = string.Join(string.Empty, parts.Skip(1));
            }
            else
            {
                Name = string.Empty;
                Endpoint = address;
            }
        }

        public BrokerAddress(string name, string? endpoint)
        {
            Name = name;
            Endpoint = endpoint;
        }

        public string Name { get; set; }

        public string? Endpoint { get; set; }

        public override string ToString()
        {
            return Name + "$" + Endpoint;
        }
    }

    public struct OperateResult : IEqualityComparer<OperateResult>
    {
        private readonly OperateError? _operateError = null;

        public OperateResult(bool succeeded, Exception? exception = null, OperateError? error = null)
        {
            Succeeded = succeeded;
            Exception = exception;
            _operateError = error;
        }

        public bool Succeeded { get; set; }

        public Exception? Exception { get; set; }

        public static OperateResult Success => new(true);

        public static OperateResult Failed(Exception ex, OperateError? errors = null)
        {
            return new(false, ex, errors);
        }

        public override string ToString()
        {
            return Succeeded ? "Succeeded" : $"Failed : {_operateError?.Code}";
        }

        public bool Equals(OperateResult x, OperateResult y)
        {
            return x.Succeeded == y.Succeeded;
        }

        public int GetHashCode(OperateResult obj)
        {
            return HashCode.Combine(obj._operateError, obj.Succeeded, obj.Exception);
        }
    }


    /// <summary>
    /// Encapsulates an error from the operate subsystem.
    /// </summary>
    public record struct OperateError
    {
        /// <summary>
        /// Gets or sets ths code for this error.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the description for this error.
        /// </summary>
        public string Description { get; set; }
    }

}
