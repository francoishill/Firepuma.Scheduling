using Azure.Messaging.ServiceBus;
using Firepuma.Scheduling.Domain.Features.Scheduling.ValueObjects;
using Firepuma.Scheduling.Domain.Infrastructure.MessageBus;
using Firepuma.Scheduling.Domain.Infrastructure.MessageBus.BusMessages;
using Firepuma.Scheduling.Domain.Infrastructure.MessageBus.Results;
using Firepuma.Scheduling.Infrastructure.Infrastructure.MessageBus.Mappings;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Firepuma.Scheduling.Infrastructure.Infrastructure.MessageBus.Sender;

internal class ClientAppBusMessageSender : IClientAppBusMessageSender
{
    private readonly ILogger<ClientAppBusMessageSender> _logger;
    private readonly Dictionary<ClientApplicationId, ServiceBusSender> _sendersMap;

    public ClientAppBusMessageSender(
        ILogger<ClientAppBusMessageSender> logger,
        Dictionary<ClientApplicationId, ServiceBusSender> sendersMap)
    {
        _logger = logger;
        _sendersMap = sendersMap;
    }

    private bool TryGetSender(ClientApplicationId applicationId, out ServiceBusSender? sender)
    {
        if (!_sendersMap.TryGetValue(applicationId, out sender))
        {
            _logger.LogWarning("Unable to find service bus sender for applicationId '{AppId}', available Ids are: {Ids}", applicationId, string.Join(", ", _sendersMap.Keys));
            return false;
        }

        return true;
    }

    public async Task<SentMessageToApplicationResult> SendMessageToApplicationAsync(
        ClientApplicationId applicationId,
        string correlationId,
        ISchedulingBusMessage messageDto,
        CancellationToken cancellationToken)
    {
        if (!TryGetSender(applicationId, out var serviceBusSender))
        {
            throw new InvalidOperationException($"Unable to find service bus sender for application '{applicationId}' in the registered applications");
        }

        var messageId = Guid.NewGuid().ToString();
        var messageTypeName = SchedulingBusMessageMappings.GetMessageTypeName(messageDto);

        var busMessage = new ServiceBusMessage(JsonConvert.SerializeObject(messageDto, new Newtonsoft.Json.Converters.StringEnumConverter()))
        {
            MessageId = messageId,
            ApplicationProperties =
            {
                [SchedulingBusMessageMappings.BUS_MESSAGE_TYPE_PROPERTY_KEY] = messageTypeName,
            },
            CorrelationId = correlationId,
        };

        _logger.LogInformation(
            "Sending message with messageId {MessageId}, messageType {Type}, correlationId {CorrelationId}",
            busMessage.MessageId, messageTypeName, correlationId);

        await serviceBusSender!.SendMessageAsync(busMessage, cancellationToken);

        _logger.LogInformation(
            "Sent message with messageId {MessageId}, messageType {Type}, correlationId {CorrelationId}",
            busMessage.MessageId, messageTypeName, correlationId);

        return new SentMessageToApplicationResult
        {
            MessageId = messageId,
            MessageTypeName = messageTypeName,
        };
    }
}