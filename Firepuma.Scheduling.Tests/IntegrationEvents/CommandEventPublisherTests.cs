using Firepuma.CommandsAndQueries.Abstractions.Commands;
using Firepuma.Scheduling.Domain.Plumbing.CommandHandling.Repositories;
using Firepuma.Scheduling.Domain.Plumbing.CommandHandling.Services;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Abstractions;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Services;
using Firepuma.Scheduling.Tests.IntegrationEvents.Helpers;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit.Abstractions;

namespace Firepuma.Scheduling.Tests.IntegrationEvents;

public class CommandEventPublisherTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public CommandEventPublisherTests(
        ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Theory]
    [MemberData(nameof(OutgoingIntegrationEventInstancesMemberData))]
    public async Task All_OutgoingIntegrationEvents_sent_via_transport(Type eventPayloadType)
    {
        // Arrange
        var integrationEventTransport = Substitute.For<IIntegrationEventTransport>();
        var publisher = new CommandEventPublisher(
            Substitute.For<ILogger<CommandEventPublisher>>(),
            Substitute.For<ICommandContext>(),
            Substitute.For<ICommandExecutionRepository>(),
            integrationEventTransport);
        var eventPayload = Activator.CreateInstance(eventPayloadType)!;

        // Act
        _testOutputHelper.WriteLine($"EventPayloadType = {eventPayloadType}");
        await publisher.PublishAsync(new MockEmptyCommand(), eventPayload, new SendEventSelfTarget(), CancellationToken.None);

        // Assert
        await integrationEventTransport
            .ReceivedWithAnyArgs(1)
            .SendAsync(default!, new SendEventSelfTarget(), default);
    }

    [Fact] public void OutgoingIntegrationEventInstancesMemberData_enumerable_is_not_empty() => Assert.NotEmpty(OutgoingIntegrationEventInstancesMemberData);

    public static IEnumerable<object[]> OutgoingIntegrationEventInstancesMemberData =>
        IntegrationEventsTestHelper.CreateEmptyNewOutgoingIntegrationEvents().Select(x => new object[] { x.EventClassType });

    internal class MockEmptyCommand : BaseCommand
    {
    }
}