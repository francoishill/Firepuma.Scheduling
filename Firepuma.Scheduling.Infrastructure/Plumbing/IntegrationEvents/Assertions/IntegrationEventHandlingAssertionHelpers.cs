using System.Reflection;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Attributes;
using Firepuma.Scheduling.Domain.Plumbing.Reflection;

namespace Firepuma.Scheduling.Infrastructure.Plumbing.IntegrationEvents.Assertions;

public static class IntegrationEventHandlingAssertionHelpers
{
    public static IEnumerable<Type> GetAllIncomingIntegrationEventTypes() => ReflectionHelpers.GetAllLoadableTypes().Where(IsIncomingIntegrationEventRequest);
    public static IEnumerable<Type> GetAllOutgoingIntegrationEventTypes() => ReflectionHelpers.GetAllLoadableTypes().Where(IsOutgoingIntegrationEventRequest);

    private static bool IsIncomingIntegrationEventRequest(Type type)
    {
        return !type.IsAbstract
               && (IncomingIntegrationEventTypeAttribute?)type.GetCustomAttribute(typeof(IncomingIntegrationEventTypeAttribute)) != null;
    }

    private static bool IsOutgoingIntegrationEventRequest(Type type)
    {
        return !type.IsAbstract
               && (OutgoingIntegrationEventTypeAttribute?)type.GetCustomAttribute(typeof(OutgoingIntegrationEventTypeAttribute)) != null;
    }
}