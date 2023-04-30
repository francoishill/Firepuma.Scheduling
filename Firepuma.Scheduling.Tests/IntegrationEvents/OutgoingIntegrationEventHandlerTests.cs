using System.Reflection;
using AutoMapper.Internal;
using Firepuma.EventMediation.IntegrationEvents.Helpers;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Abstractions;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Attributes;
using Firepuma.Scheduling.Domain.Plumbing.Reflection;
using Firepuma.Scheduling.Infrastructure.Plumbing.IntegrationEvents.Assertions;
using Firepuma.Scheduling.Tests.IntegrationEvents.Helpers;
using Xunit.Abstractions;

namespace Firepuma.Scheduling.Tests.IntegrationEvents;

public class OutgoingIntegrationEventHandlerTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public OutgoingIntegrationEventHandlerTests(
        ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void No_duplicate_OutgoingIntegrationEvent_types()
    {
        // Arrange

        // Act
        _testOutputHelper.WriteLine($"Outgoing event types = {string.Join(", ", OutgoingIntegrationEventTypeStringsOnly)}");
        var duplicates = OutgoingIntegrationEventTypeStringsOnly
            .GroupBy(eventType => eventType)
            .Where(group => group.Count() > 1)
            .ToList();

        // Assert
        foreach (var duplicate in duplicates)
        {
            _testOutputHelper.WriteLine($"Duplicate outgoing event type found: {duplicate.Key}");
        }

        Assert.Empty(duplicates);
    }

    [Fact]
    public void GetDuplicateOutgoingIntegrationEventTypeAttributes_Should_be_empty()
    {
        // Arrange
        // Act
        var duplicateIntegrationEventTypes = EventTypeHelpers.GetDuplicateIntegrationEventTypeAttributes<OutgoingIntegrationEventTypeAttribute>(
            ReflectionHelpers.GetAllLoadableTypes(),
            attribute => attribute.IntegrationEventType);

        // Assert
        Assert.Empty(duplicateIntegrationEventTypes);
    }

    [Fact]
    public void OutgoingIntegrationEventType_should_extend_BaseOutgoingIntegrationEvent()
    {
        // Arrange
        // Act
        var eventsNotExtendingBaseEvent = IntegrationEventHandlingAssertionHelpers
            .GetAllOutgoingIntegrationEventTypes()
            .Where(type => !type.BaseClassesAndInterfaces().Contains(typeof(BaseOutgoingIntegrationEvent)))
            .ToList();

        // Assert
        if (eventsNotExtendingBaseEvent.Count > 0)
        {
            throw new Exception(string.Join(". ", eventsNotExtendingBaseEvent.Select(type =>
                $"{type.FullName} should extend {nameof(BaseOutgoingIntegrationEvent)} because it has " +
                $"{nameof(OutgoingIntegrationEventTypeAttribute)}")));
        }
    }

    [Fact]
    public void Events_extending_BaseOutgoingIntegrationEvent_should_have_OutgoingIntegrationEventType()
    {
        // Arrange
        // Act
        var typesWithoutAttribute = ReflectionHelpers
            .GetAllLoadableTypes()
            .Where(type => type.BaseClassesAndInterfaces().Contains(typeof(BaseOutgoingIntegrationEvent))
                           && (OutgoingIntegrationEventTypeAttribute?)type.GetCustomAttribute(typeof(OutgoingIntegrationEventTypeAttribute)) == null)
            .ToList();

        // Assert
        if (typesWithoutAttribute.Count > 0)
        {
            throw new Exception(string.Join(". ", typesWithoutAttribute.Select(type =>
                $"{type.FullName} should have {nameof(OutgoingIntegrationEventTypeAttribute)} because " +
                $"it extends {nameof(BaseOutgoingIntegrationEvent)}")));
        }
    }

    [Fact] public void IntegrationEventTypesInheritingBaseOutgoingIntegrationEvent_enumerable_is_not_empty() => Assert.NotEmpty(IntegrationEventTypesInheritingBaseOutgoingIntegrationEvent);
    [Fact] public void OutgoingIntegrationEventTypeStringsOnly_enumerable_is_not_empty() => Assert.NotEmpty(OutgoingIntegrationEventTypeStringsOnly);

    public static IEnumerable<object[]> IntegrationEventTypesInheritingBaseOutgoingIntegrationEvent =>
        IntegrationEventsTestHelper.GetClassTypesInheritingBaseClass<BaseOutgoingIntegrationEvent>().Select(type => new object[] { type });

    private static IEnumerable<string> OutgoingIntegrationEventTypeStringsOnly =>
        IntegrationEventsTestHelper.CreateEmptyNewOutgoingIntegrationEvents().Select(x => x.EventTypeString);
}