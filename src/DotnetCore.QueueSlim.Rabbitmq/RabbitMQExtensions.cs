using CloudNative.CloudEvents.Core;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mime;

namespace CloudNative.CloudEvents.RabbitMQ
{
    public class RabbitMQMessage
    {
        public RabbitMQMessage(IDictionary<string, object?> headers, ReadOnlyMemory<byte> body)
        {
            this.Headers = headers;
            this.Body = body;
        }

        public RabbitMQMessage()
        {

        }

        public IDictionary<string, object?> Headers { get; set; } = new Dictionary<string, object?>();

        public ReadOnlyMemory<byte> Body { get; set; } = new byte[0];
    }


    // 区别是
    // 结构化，就会序列化整个 CloudEvent
    // 二进制，只会序列化 CloudEvent.Data 属性

    public static class RabbitMQExtensions
    {
        // This is internal in CloudEventsSpecVersion.
        private const string SpecVersionAttributeName = "specversion";

        internal const string RabbitMQHeaderUnderscorePrefix = "cloudEvents_";
        internal const string RabbitMQHeaderColonPrefix = "cloudEvents:";

        internal const string SpecVersionRabbitMQHeaderWithUnderscore = RabbitMQHeaderUnderscorePrefix + SpecVersionAttributeName;
        internal const string SpecVersionRabbitMQHeaderWithColon = RabbitMQHeaderColonPrefix + SpecVersionAttributeName;

        internal const string ContentTypeHeaderName = "datacontenttype";

        internal const string SpecVersionContentTypeWithUnderscore = RabbitMQHeaderUnderscorePrefix + ContentTypeHeaderName;
        internal const string SpecVersionContentTypeWithColon = RabbitMQHeaderColonPrefix + ContentTypeHeaderName;

        internal const string TimestampKey = "time";

        /// <summary>
        /// Indicates whether this <see cref="Message"/> holds a single CloudEvent.
        /// </summary>
        /// <remarks>
        /// This method returns false for batch requests, as they need to be parsed differently.
        /// </remarks>
        /// <param name="message">The message to check for the presence of a CloudEvent. Must not be null.</param>
        /// <returns>true, if the request is a CloudEvent</returns>
        public static bool IsCloudEvent(this RabbitMQMessage message) =>
            HasCloudEventsContentType(Validation.CheckNotNull(message, nameof(message)), out _) ||
            message.Headers.ContainsKey(SpecVersionRabbitMQHeaderWithUnderscore) ||
            message.Headers.ContainsKey(SpecVersionRabbitMQHeaderWithColon);

        /// <summary>
        /// Converts this AMQP message into a CloudEvent object.
        /// </summary>
        /// <param name="message">The AMQP message to convert. Must not be null.</param>
        /// <param name="formatter">The event formatter to use to parse the CloudEvent. Must not be null.</param>
        /// <param name="extensionAttributes">The extension attributes to use when parsing the CloudEvent. May be null.</param>
        /// <returns>A reference to a validated CloudEvent instance.</returns>
        public static CloudEvent ToCloudEvent(
            this RabbitMQMessage message,
            CloudEventFormatter formatter,
            params CloudEventAttribute[]? extensionAttributes) =>
            ToCloudEvent(message, formatter, (IEnumerable<CloudEventAttribute>?)extensionAttributes);

        /// <summary>
        /// Converts this AMQP message into a CloudEvent object.
        /// </summary>
        /// <param name="message">The AMQP message to convert. Must not be null.</param>
        /// <param name="formatter">The event formatter to use to parse the CloudEvent. Must not be null.</param>
        /// <param name="extensionAttributes">The extension attributes to use when parsing the CloudEvent. May be null.</param>
        /// <returns>A reference to a validated CloudEvent instance.</returns>
        public static CloudEvent ToCloudEvent(
            this RabbitMQMessage message,
            CloudEventFormatter formatter,
            IEnumerable<CloudEventAttribute>? extensionAttributes)
        {
            Validation.CheckNotNull(message, nameof(message));
            Validation.CheckNotNull(formatter, nameof(formatter));

            // 有 contenttype，是 structrured 模式，直接转换
            if (HasCloudEventsContentType(message, out var contentType))
            {
                return formatter.DecodeStructuredModeMessage(new MemoryStream(message.Body.ToArray()),
                    new ContentType(contentType),
                    extensionAttributes);
            }
            else // 需要自己解析 ContentType
            {
                var propertyMap = message.Headers;
                if (!propertyMap.TryGetValue(SpecVersionRabbitMQHeaderWithUnderscore, out var versionId) &&
                    !propertyMap.TryGetValue(SpecVersionRabbitMQHeaderWithColon, out versionId)
                    )
                {
                    throw new ArgumentException("Request is not a CloudEvent");
                }

                var version = CloudEventsSpecVersion.FromVersionId(versionId as string)
                    ?? throw new ArgumentException($"Unknown CloudEvents spec version '{versionId}'", nameof(message));


                var cloudEvent = new CloudEvent(version, extensionAttributes) { };

                // 遍历处理 header
                foreach (var property in propertyMap)
                {
                    // 过滤没有 prefix 的key
                    if (!(property.Key is string key &&
                        (key.StartsWith(RabbitMQHeaderColonPrefix) || key.StartsWith(RabbitMQHeaderUnderscorePrefix))))
                    {
                        continue;
                    }
                    // Note: both prefixes have the same length. If we ever need any prefixes with a different length, we'll need to know which
                    // prefix we're looking at.
                    string attributeName = key.Substring(RabbitMQHeaderUnderscorePrefix.Length).ToLowerInvariant();

                    // We've already dealt with the spec version.
                    if (attributeName == CloudEventsSpecVersion.SpecVersionAttribute.Name)
                    {
                        continue;
                    }

                    // Timestamps are serialized via DateTime instead of DateTimeOffset.
                    if (attributeName == TimestampKey)
                    {
                        var dt = DateTimeOffset.Parse(Convert.ToString(property.Value));
                        //if (dt.Kind != DateTimeKind.Utc)
                        //{
                        //    // This should only happen for MinValue and MaxValue...
                        //    // just respecify as UTC. (We could add validation that it really
                        //    // *is* MinValue or MaxValue if we wanted to.)
                        //    dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                        //}
                        cloudEvent[attributeName] = (DateTimeOffset)dt;
                    }
                    // URIs are serialized as strings, but we need to convert them back to URIs.
                    // It's simplest to let CloudEvent do this for us.
                    else if (property.Value is string text)
                    {
                        cloudEvent.SetAttributeFromString(attributeName, text);
                    }
                    else
                    {
                        cloudEvent[attributeName] = property.Value;
                    }
                }

                if (message.Body.Length > 0)
                {
                    formatter.DecodeBinaryModeEventData(message.Body, cloudEvent);
                }


                return Validation.CheckCloudEventArgument(cloudEvent, nameof(message));
            }
        }

        private static bool HasCloudEventsContentType(RabbitMQMessage message, out string? contentType)
        {
            if (!TryGetAttribute(message.Headers, ContentTypeHeaderName, out var outContentType))
            {
                contentType = null;
                return false;
            }

            contentType = Convert.ToString(outContentType);

            return MimeUtilities.IsCloudEventsContentType(contentType);
        }

        /// <summary>
        /// Converts a CloudEvent to <see cref="Message"/> using the default property prefix. Versions released prior to March 2023
        /// use a default property prefix of "cloudEvents:". Versions released from March 2023 onwards use a property prefix of "cloudEvents_".
        /// Code wishing to express the prefix explicitly should use <see cref="ToRabbitMQMessageWithColonPrefix(CloudEvent, ContentMode, CloudEventFormatter)"/> or
        /// <see cref="ToRabbitMQMessageWithUnderscorePrefix(CloudEvent, ContentMode, CloudEventFormatter)"/>.
        /// </summary>
        /// <param name="cloudEvent">The CloudEvent to convert. Must not be null, and must be a valid CloudEvent.</param>
        /// <param name="contentMode">Content mode. Structured or binary.</param>
        /// <param name="formatter">The formatter to use within the conversion. Must not be null.</param>
        public static RabbitMQMessage ToRabbitMQMessage(this CloudEvent cloudEvent, ContentMode contentMode, CloudEventFormatter formatter) =>
            ToRabbitMQMessage(cloudEvent, contentMode, formatter, RabbitMQHeaderColonPrefix);

        /// <summary>
        /// Converts a CloudEvent to <see cref="Message"/> using a property prefix of "cloudEvents_". This prefix was introduced as the preferred
        /// prefix for the AMQP binding in August 2022.
        /// </summary>
        /// <param name="cloudEvent">The CloudEvent to convert. Must not be null, and must be a valid CloudEvent.</param>
        /// <param name="contentMode">Content mode. Structured or binary.</param>
        /// <param name="formatter">The formatter to use within the conversion. Must not be null.</param>
        public static RabbitMQMessage ToRabbitMQMessageWithUnderscorePrefix(this CloudEvent cloudEvent, ContentMode contentMode, CloudEventFormatter formatter) =>
            ToRabbitMQMessage(cloudEvent, contentMode, formatter, RabbitMQHeaderUnderscorePrefix);

        /// <summary>
        /// Converts a CloudEvent to <see cref="Message"/> using a property prefix of "cloudEvents:". This prefix
        /// is a legacy retained only for compatibility purposes; it can't be used by JMS due to constraints in JMS property names.
        /// </summary>
        /// <param name="cloudEvent">The CloudEvent to convert. Must not be null, and must be a valid CloudEvent.</param>
        /// <param name="contentMode">Content mode. Structured or binary.</param>
        /// <param name="formatter">The formatter to use within the conversion. Must not be null.</param>
        public static RabbitMQMessage ToRabbitMQMessageWithColonPrefix(this CloudEvent cloudEvent, ContentMode contentMode, CloudEventFormatter formatter) =>
            ToRabbitMQMessage(cloudEvent, contentMode, formatter, RabbitMQHeaderColonPrefix);

        private static RabbitMQMessage ToRabbitMQMessage(CloudEvent cloudEvent,
            ContentMode contentMode,
            CloudEventFormatter formatter,
            string prefix)
        {
            Validation.CheckCloudEventArgument(cloudEvent, nameof(cloudEvent));
            Validation.CheckNotNull(formatter, nameof(formatter));

            var messageResult = new RabbitMQMessage();


            switch (contentMode)
            {
                case ContentMode.Structured:

                    // 这种模式下，其实可以不用管 header
                    messageResult.Body = BinaryDataUtilities.AsArray(formatter.EncodeStructuredModeMessage(cloudEvent, out var contentType));
                    // TODO: What about the other parts of the content type?
                    // properties = new Properties { ContentType = contentType.MediaType };
                    // for example: application/cloudevents+json
                    messageResult.Headers.Add(prefix + ContentTypeHeaderName, contentType.MediaType);
                    break;
                case ContentMode.Binary:
                    // 这种情况下，要管 header
                    messageResult.Headers = MapHeaders(cloudEvent, prefix);
                    messageResult.Body = BinaryDataUtilities.AsArray(formatter.EncodeBinaryModeEventData(cloudEvent));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(contentMode), $"Unsupported content mode: {contentMode}");
            }
            return messageResult;
        }

        private static IDictionary<string, object?> MapHeaders(CloudEvent cloudEvent, string prefix)
        {
            var properties = new Dictionary<string, object?>
            {
                { prefix + SpecVersionAttributeName, cloudEvent.SpecVersion.VersionId }
            };

            foreach (var pair in cloudEvent.GetPopulatedAttributes())
            {
                var attribute = pair.Key;

                // The content type is specified elsewhere.
                //if (attribute == cloudEvent.SpecVersion.DataContentTypeAttribute)
                //{
                //    continue;
                //}

                string propKey = prefix + attribute.Name;

                // TODO: Check that AMQP can handle byte[], bool and int values
                object propValue = pair.Value switch
                {
                    Uri uri => uri.ToString(),
                    // AMQPNetLite doesn't support DateTimeOffset values, so convert to UTC.
                    // That means we can't roundtrip events with non-UTC timestamps, but that's not awful.
                    DateTimeOffset dto => dto.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    _ => pair.Value
                };
                properties.Add(propKey, propValue);
            }
            return properties;
        }

        private static bool TryGetAttribute(IDictionary<string, object?> propertyMap, string key, out string? stringValue)
        {
            if (!propertyMap.TryGetValue(RabbitMQHeaderUnderscorePrefix + key, out var value) &&
                    !propertyMap.TryGetValue(RabbitMQHeaderColonPrefix + key, out value)
                    )
            {
                stringValue = string.Empty;
                return false;
            }

            stringValue = Convert.ToString(value);

            return true;
        }
    }
}
