// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Kafka;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using DotnetCore.QueueSlim.Internal;
using DotnetCore.QueueSlim.Utils;
using Microsoft.Extensions.Options;
using System.Text;

namespace DotNetCore.CAP.Kafka
{
    public class KafkaConsumerClient : IConsumerClient
    {
        private static readonly SemaphoreSlim ConnectionLock = new(initialCount: 1, maxCount: 1);

        private readonly string _groupId;
        private readonly KafkaOptions _kafkaOptions;
        private IConsumer<string, byte[]>? _consumerClient;
        private CloudEventFormatter _formatter;
        private readonly bool _autoUnsubscribe;

        public KafkaConsumerClient(string groupId, IOptions<KafkaOptions> options,
            CloudEventFormatter formatter,
            bool autoUnsubscribe
            )
        {
            _groupId = groupId;
            _kafkaOptions = options.Value ?? throw new ArgumentNullException(nameof(options));
            _formatter = formatter;
            _autoUnsubscribe = autoUnsubscribe;
        }

        public Func<CloudEvent, object?, Task>? OnMessageCallback { get; set; }

        public Action<LogMessageEventArgs>? OnLogCallback { get; set; }

        public BrokerAddress BrokerAddress => new("Kafka", _kafkaOptions.Servers);

        public ICollection<string> FetchTopics(IEnumerable<string> topicNames)
        {
            if (topicNames == null)
            {
                throw new ArgumentNullException(nameof(topicNames));
            }

            var regexTopicNames = topicNames.Select(Helper.WildcardToRegex).ToList();

            try
            {
                var config = new AdminClientConfig(_kafkaOptions.MainConfig) { BootstrapServers = _kafkaOptions.Servers };

                using var adminClient = new AdminClientBuilder(config).Build();

                adminClient.CreateTopicsAsync(regexTopicNames.Select(x => new TopicSpecification
                {
                    Name = x
                })).GetAwaiter().GetResult();
            }
            catch (CreateTopicsException ex) when (ex.Message.Contains("already exists"))
            {
            }
            catch (Exception ex)
            {
                var logArgs = new LogMessageEventArgs
                {
                    LogType = MqLogType.ConsumeError,
                    Reason = $"An error was encountered when automatically creating topic! -->" + ex.Message
                };
                OnLogCallback!(logArgs);
            }

            return regexTopicNames;
        }

        public void Subscribe(IEnumerable<string> topics)
        {
            if (topics == null)
            {
                throw new ArgumentNullException(nameof(topics));
            }

            Connect();

            _consumerClient!.Subscribe(topics);
        }

        public void Listening(TimeSpan timeout, CancellationToken cancellationToken)
        {
            Connect();

            while (!cancellationToken.IsCancellationRequested)
            {
                ConsumeResult<string, byte[]> consumerResult;

                try
                {
                    consumerResult = _consumerClient!.Consume(timeout);
                    if (consumerResult == null) continue;
                }
                catch (ConsumeException e) when (_kafkaOptions.RetriableErrorCodes.Contains(e.Error.Code))
                {
                    var logArgs = new LogMessageEventArgs
                    {
                        LogType = MqLogType.ConsumeRetries,
                        Reason = e.Error.ToString()
                    };
                    OnLogCallback!(logArgs);

                    continue;
                }

                if (consumerResult.IsPartitionEOF || consumerResult.Message.Value == null) continue;

                var headers = new Dictionary<string, string?>(consumerResult.Message.Headers.Count);
                foreach (var header in consumerResult.Message.Headers)
                {
                    var val = header.GetValueBytes();
                    headers.Add(header.Key, val != null ? Encoding.UTF8.GetString(val) : null);
                }

                headers.Add(MessageConstants.Names.Group, _groupId);

                if (_kafkaOptions.CustomHeaders != null)
                {
                    var customHeaders = _kafkaOptions.CustomHeaders(consumerResult);
                    foreach (var customHeader in customHeaders)
                    {
                        headers[customHeader.Key] = customHeader.Value;
                    }
                }

                // var message = new TransportMessage(headers, consumerResult.Message.Value);

                var cloudEvent = consumerResult.Message!.ToCloudEvent(_formatter);

                OnMessageCallback!(cloudEvent, consumerResult).GetAwaiter().GetResult();
            }

            // TODO 取消订阅？
            // ReSharper disable once FunctionNeverReturns
        }

        public void Commit(object? sender)
        {
            _consumerClient!.Commit((ConsumeResult<string, byte[]>)sender!);
        }

        public void Reject(object? sender)
        {
            _consumerClient!.Assign(_consumerClient.Assignment);
        }

        public void Dispose()
        {
            _consumerClient?.Dispose();
        }

        public void Connect()
        {
            if (_consumerClient != null)
            {
                return;
            }

            ConnectionLock.Wait();

            try
            {
                if (_consumerClient == null)
                {
                    var config = new ConsumerConfig(new Dictionary<string, string>(_kafkaOptions.MainConfig));
                    config.BootstrapServers ??= _kafkaOptions.Servers;
                    config.GroupId ??= _groupId;
                    config.AutoOffsetReset ??= AutoOffsetReset.Earliest;
                    config.AllowAutoCreateTopics ??= true;
                    config.EnableAutoCommit ??= false;
                    config.LogConnectionClose ??= false;

                    _consumerClient = BuildConsumer(config);
                }
            }
            finally
            {
                ConnectionLock.Release();
            }
        }

        protected virtual IConsumer<string, byte[]> BuildConsumer(ConsumerConfig config)
        {
            return new ConsumerBuilder<string, byte[]>(config)
                .SetErrorHandler(ConsumerClient_OnConsumeError)
                .Build();
        }

        private void ConsumerClient_OnConsumeError(IConsumer<string, byte[]> consumer, Error e)
        {
            var logArgs = new LogMessageEventArgs
            {
                LogType = MqLogType.ServerConnError,
                Reason = $"An error occurred during connect kafka --> {e.Reason}"
            };
            OnLogCallback!(logArgs);
        }

        public void Unsubscribe(IEnumerable<string> topics)
        {
            if (topics == null)
            {
                throw new ArgumentNullException(nameof(topics));
            }

            Connect();
            _consumerClient!.Unsubscribe();
        }
    }
}