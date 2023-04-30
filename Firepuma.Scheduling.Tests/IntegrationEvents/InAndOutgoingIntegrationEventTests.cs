using System.Reflection;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Attributes;
using Firepuma.Scheduling.Domain.Plumbing.Reflection;

namespace Firepuma.Scheduling.Tests.IntegrationEvents;

public class InAndOutgoingIntegrationEventTests
{
    [Fact]
    public void EventType_equal_when_both_incoming_and_outgoing_attributes()
    {
        // Arrange
        var typesWithBoth = ReflectionHelpers.GetAllLoadableTypes()
            .Select(type => new
            {
                Type = type,
                IncomingAttribute = (IncomingIntegrationEventTypeAttribute?)type.GetCustomAttribute(typeof(IncomingIntegrationEventTypeAttribute)),
                OutgoingAttribute = (OutgoingIntegrationEventTypeAttribute?)type.GetCustomAttribute(typeof(OutgoingIntegrationEventTypeAttribute)),
            })
            .Where(x => x.IncomingAttribute != null && x.OutgoingAttribute != null)
            .ToList();

        //TODO: uncomment this if we ever add integration events with both IncomingIntegrationEventType and OutgoingIntegrationEventType attributes (for events handled by self)
        // Assert.NotEmpty(typesWithBoth);

        // Act
        var typesWhereTypesDiffer = typesWithBoth
            .Where(x => x.IncomingAttribute!.IntegrationEventType != x.OutgoingAttribute!.IntegrationEventType)
            .ToList();

        // Assert
        if (typesWhereTypesDiffer.Count > 0)
        {
            var summaryPerType = typesWhereTypesDiffer
                .Select(x => $"Outgoing={x.OutgoingAttribute!.IntegrationEventType} and " +
                             $"Incoming={x.IncomingAttribute!.IntegrationEventType} " +
                             $"should not be different on {x.Type.FullName}");

            throw new Exception(
                $"{string.Join(". ", summaryPerType)}. Integration events with both {nameof(OutgoingIntegrationEventTypeAttribute)} and " +
                $"{nameof(IncomingIntegrationEventTypeAttribute)} should have the same value for " +
                $"{nameof(OutgoingIntegrationEventTypeAttribute.IntegrationEventType)}");
        }

        Assert.Empty(typesWhereTypesDiffer);
    }
}