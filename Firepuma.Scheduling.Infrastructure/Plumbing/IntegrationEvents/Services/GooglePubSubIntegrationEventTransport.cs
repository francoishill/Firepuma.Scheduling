using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.RegularExpressions;
using Firepuma.BusMessaging.GooglePubSub.Services;
using Firepuma.EventMediation.IntegrationEvents.Constants;
using Firepuma.EventMediation.IntegrationEvents.ValueObjects;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Abstractions;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Config;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Services;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Firepuma.Scheduling.Infrastructure.Plumbing.IntegrationEvents.Services;

/// <summary>
/// This class is responsible for creating a new Google <see cref="PubsubMessage"/>, serializing the
/// payload, adding attributes, determining the topic to send to, and finally sending the bus message.
/// </summary>
// ReSharper disable ClassNeverInstantiated.Global
public class GooglePubSubIntegrationEventTransport : IIntegrationEventTransport
{
    private readonly ILogger<GooglePubSubIntegrationEventTransport> _logger;
    private readonly IPublisherClientCache _publisherClientCache;
    private readonly TopicName _selfTopicName;

    public GooglePubSubIntegrationEventTransport(
        IOptions<IntegrationEventsOptions> options,
        ILogger<GooglePubSubIntegrationEventTransport> logger,
        IPublisherClientCache publisherClientCache)
    {
        _logger = logger;
        _publisherClientCache = publisherClientCache;

        _selfTopicName = TopicName.FromProjectTopic(options.Value.SelfProjectId, options.Value.SelfTopicId);
    }

    public async Task SendAsync(
        IntegrationEventEnvelope eventEnvelope,
        ISendEventTarget target,
        CancellationToken cancellationToken)
    {
        var messageType = eventEnvelope.EventType;

        var eventEnvelopeJson = JsonSerializer.Serialize(eventEnvelope);
        var message = new PubsubMessage
        {
            Data = ByteString.CopyFromUtf8(eventEnvelopeJson),
        };

        var attributes = new Dictionary<string, string>
        {
            [FirepumaAttributeKeys.MESSAGE_TYPE] = messageType,
        };

        message.Attributes.Add(attributes);

        var topic = GetTopicForMessageType(messageType, target);
        var cacheKey = $"{topic.ProjectId}/{topic.TopicId}";
        var publisher = await _publisherClientCache.GetPublisherClient(topic, cacheKey, cancellationToken);

        _logger.LogDebug(
            "Obtained publisher for message {MessageType}, project: {Project}, topic: {Topic}",
            messageType, publisher.TopicName.ProjectId, publisher.TopicName.TopicId);

        try
        {
            var sentMessageId = await publisher.PublishAsync(message);

            _logger.LogInformation(
                "Message {Id} was successfully published at {Time}, project: {Project}, topic: {Topic}",
                sentMessageId, DateTime.UtcNow.ToString("O"), publisher.TopicName.ProjectId, publisher.TopicName.TopicId);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Unable to publish message to topic {Topic}, integration event Id: {EventId}, Type: {EventType}",
                topic.ToString(), eventEnvelope.EventId, eventEnvelope.EventType);
            throw;
        }
    }

    private TopicName GetTopicForMessageType(
        string messageType,
        ISendEventTarget target)
    {
        switch (target)
        {
            case SendEventSelfTarget:
                return _selfTopicName;

            case SendEventReplyTarget replyTarget:
                if (TryParseTopicNameFromEventReplyToAddress(replyTarget.EventReplyToAddress, out var parsedTopic))
                {
                    return parsedTopic;
                }

                _logger.LogWarning(
                    "Unable to create Google TopicName from EventReplyToAddress '{EventReplyToAddress}'",
                    replyTarget.EventReplyToAddress);

                break;
        }

        _logger.LogError("Message type '{MessageType}' does not have a configured pubsub topic", messageType);
        throw new Exception($"Message type '{messageType}' does not have a configured pubsub topic");
    }

    public static bool TryParseTopicNameFromEventReplyToAddress(
        string eventReplyToAddress,
        [NotNullWhen(true)] out TopicName? topicName)
    {
        // Google Project ID requirements:
        // Project ID can have lowercase letters, digits or hyphens.
        // It must start with a lowercase letter and end with a letter or number.

        // Google Topic ID requirements:
        // ID must start with a letter, and contain only the following characters:
        //   letters, numbers, dashes (-), full points (.), underscores (_), tildes (~), percents (%) or plus signs (+).
        // Cannot start with goog.

        var googlePubSubPattern = new Regex(
            @"googlepubsub:(?<projectId>[a-z][a-z0-9\-]+[a-z0-9])/(?<topicId>[a-zA-Z][a-zA-Z0-9\-\._\~\%\+]+)",
            RegexOptions.Compiled);

        var googleMatch = googlePubSubPattern.Match(eventReplyToAddress);

        if (googleMatch.Success)
        {
            var projectId = googleMatch.Groups["projectId"].Value;
            var topicId = googleMatch.Groups["topicId"].Value;

            topicName = TopicName.FromProjectTopic(projectId, topicId);
            return true;
        }

        topicName = null;
        return false;
    }
}