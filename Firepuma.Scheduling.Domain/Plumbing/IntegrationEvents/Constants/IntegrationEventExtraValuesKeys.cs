namespace Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Constants;

/// <summary>
/// These are keys used for storing IntegrationEvent information in the ExtraValues of
/// a CommandExecutionEvent. Refer to <see cref="Services.CommandEventPublisher"/> for its usage.
/// </summary>
[Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
public enum IntegrationEventExtraValuesKeys
{
    IntegrationEventId,
    IntegrationEventPayloadJson,
    IntegrationEventPayloadType,
    IntegrationEventPublishResultTime,
    IntegrationEventPublishResultSuccess,
    IntegrationEventPublishResultError,
    // IntegrationEventLockUntilUnixSeconds,
}