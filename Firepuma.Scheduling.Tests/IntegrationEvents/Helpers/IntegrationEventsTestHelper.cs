using System.Reflection;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Attributes;
using Firepuma.Scheduling.Domain.Plumbing.Reflection;

namespace Firepuma.Scheduling.Tests.IntegrationEvents.Helpers;

internal abstract class IntegrationEventsTestHelper
{
    public static IEnumerable<EmptyIntegrationEvent> CreateEmptyNewIncomingIntegrationEvents()
    {
        return CreateEmptyIntegrationEventsOfAllWithAttribute<IncomingIntegrationEventTypeAttribute>(attribute => attribute.IntegrationEventType);
    }

    public static IEnumerable<EmptyIntegrationEvent> CreateEmptyNewOutgoingIntegrationEvents()
    {
        return CreateEmptyIntegrationEventsOfAllWithAttribute<OutgoingIntegrationEventTypeAttribute>(attribute => attribute.IntegrationEventType);
    }

    public class EmptyIntegrationEvent
    {
        public required Type EventClassType { get; init; }
        public required string EventTypeString { get; init; }
        public required object EmptyEventPayload { get; init; }
    }

    private static IEnumerable<EmptyIntegrationEvent> CreateEmptyIntegrationEventsOfAllWithAttribute<TAttribute>(
        Func<TAttribute, string> getIntegrationEventTypeFromAttribute)
        where TAttribute : Attribute
    {
        return ReflectionHelpers.GetAllLoadableTypes()
            .Select(type => type
                .GetCustomAttributes(typeof(TAttribute), false)
                .Cast<TAttribute>()
                .SingleOrDefault() != null
                ? type
                : null)
            .Where(type => type != null)
            .Cast<Type>()
            .Select(eventPayloadType =>
            {
                var eventPayload = Activator.CreateInstance(eventPayloadType);

                if (eventPayload == null)
                {
                    throw new Exception($"eventPayload should not be null when creating instance for event payload type {eventPayloadType.Name}");
                }

                var attributeValue = (TAttribute?)eventPayload
                    .GetType()
                    .GetCustomAttribute(typeof(TAttribute));

                var integrationEventTypeFromAttribute = getIntegrationEventTypeFromAttribute(attributeValue!);


                if (integrationEventTypeFromAttribute == null)
                {
                    throw new Exception($"integrationEventTypeFromAttribute should not be null for event payload type {eventPayloadType.Name}");
                }

                return new EmptyIntegrationEvent
                {
                    EventClassType = eventPayload.GetType(),
                    EventTypeString = integrationEventTypeFromAttribute,
                    EmptyEventPayload = eventPayload,
                };
            });
    }

    public static IEnumerable<Type> GetClassTypesInheritingBaseClass<T>() where T : class
    {
        var allTypesEnumerable = ReflectionHelpers.GetAllLoadableTypes();

        var instances = allTypesEnumerable
            .Where(myType => myType is { IsClass: true, IsAbstract: false } && myType.IsSubclassOf(typeof(T)));

        return instances;
    }
}