using System.Reflection;
using System.Text.Json;
using Firepuma.CommandsAndQueries.Abstractions.Commands;
using Firepuma.EventMediation.IntegrationEvents.ValueObjects;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Abstractions;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Attributes;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Entities;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Repositories;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Services;
using Firepuma.Scheduling.Domain.Plumbing.Reflection;
using Firepuma.Scheduling.Infrastructure.Plumbing.IntegrationEvents.Assertions;
using Firepuma.Scheduling.Tests.IntegrationEvents.Helpers;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using NSubstitute;
using Xunit.Abstractions;

namespace Firepuma.Scheduling.Tests.IntegrationEvents;

public class IncomingIntegrationEventHandlerTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public IncomingIntegrationEventHandlerTests(
        ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Theory]
    [MemberData(nameof(IncomingIntegrationEventTypes))]
    public void IncomingIntegrationEventTypes_have_single_handler(Type integrationEventType)
    {
        _testOutputHelper.WriteLine($"Testing {integrationEventType.FullName ?? integrationEventType.Name}");
        var registeredHandlers = ReflectionHelpers.GetRegisteredHandlersForRequestType(integrationEventType).ToList();

        if (registeredHandlers.Count != 1)
        {
            throw new Exception(
                $"{integrationEventType.FullName} has {registeredHandlers.Count} handlers but it " +
                $"should have 1, handler types: {string.Join(", ", registeredHandlers.Select(h => h.FullName))}");
        }
    }

    [Theory]
    [MemberData(nameof(IncomingIntegrationEventTypeStringsWithPayloadInstancesMemberData))]
    public async Task All_IntegrationEvents_pass_TryHandleEventAsync(string eventType, object @event)
    {
        // the fact that IntegrationEventTypeStrings is derived from calling TryGetIntegrationEventType means that
        // it implicitly also tests that each type having an EventType mapping can also be deserialized

        // Arrange
        var mediator = Substitute.For<IMediator>();
        var integrationEventExecutionRepository = Substitute.For<IIntegrationEventExecutionRepository>();
        var eventHandler = new IntegrationEventHandler(
            Substitute.For<ILogger<IntegrationEventHandler>>(),
            mediator,
            integrationEventExecutionRepository);
        var envelope = new IntegrationEventEnvelope
        {
            EventId = ObjectId.GenerateNewId().ToString(),
            EventType = eventType,
            EventPayload = JsonSerializer.Serialize((dynamic)@event),
        };

        var eventExecution = new IntegrationEventExecution
        {
            Id = "",
            EventId = "",
            Status = default,
            StartedOn = default,
            TypeName = "",
            TypeNamespace = "",
            Payload = "",
            SourceCommandId = null,
        };

        integrationEventExecutionRepository
            .AddItemAsync(Arg.Any<IntegrationEventExecution>(), Arg.Any<CancellationToken>())
            .Returns(eventExecution);

        // Act
        _testOutputHelper.WriteLine($"EventType = {eventType}, EventPayload = {envelope.EventPayload}");
        var successfullyHandledEventType = await eventHandler.TryHandleEventAsync(envelope, CancellationToken.None);

        // Assert
        Assert.True(successfullyHandledEventType);
    }

    [Fact]
    public async Task TryHandleEventAsync_Successful_status_counts_as_handled()
    {
        // Arrange
        var logger = Substitute.For<ILogger<IntegrationEventHandler>>();
        var serviceProvider = new ServiceCollection().AddMediatR(c => c.RegisterServicesFromAssemblies(GetType().Assembly)).BuildServiceProvider();
        var mediator = new Mediator(serviceProvider);
        var integrationEventExecutionRepository = Substitute.For<IIntegrationEventExecutionRepository>();
        var handler = new IntegrationEventHandler(logger, mediator, integrationEventExecutionRepository);
        var eventEnvelope = new IntegrationEventEnvelope
        {
            EventId = "",
            EventType = "Mock/MockSuccessfulEvent",
            EventPayload = JsonSerializer.Serialize(new MockSuccessfulEvent()),
        };
        var eventExecution = new IntegrationEventExecution
        {
            Id = "",
            EventId = "",
            Status = default,
            StartedOn = default,
            TypeName = "",
            TypeNamespace = "",
            Payload = "",
            SourceCommandId = null,
        };
        integrationEventExecutionRepository
            .AddItemAsync(Arg.Any<IntegrationEventExecution>(), Arg.Any<CancellationToken>())
            .Returns(eventExecution);

        // Act
        var handled = await handler.TryHandleEventAsync(eventEnvelope, CancellationToken.None);

        // Assert
        Assert.Equal(IntegrationEventExecution.ExecutionStatus.Successful, eventExecution.Status);
        Assert.True(handled);
    }

    [Fact]
    public async Task TryHandleEventAsync_PartiallySuccessful_status_counts_as_handled()
    {
        // Arrange
        var logger = Substitute.For<ILogger<IntegrationEventHandler>>();
        var serviceProvider = new ServiceCollection().AddMediatR(c => c.RegisterServicesFromAssemblies(GetType().Assembly)).BuildServiceProvider();
        var mediator = new Mediator(serviceProvider);
        var integrationEventExecutionRepository = Substitute.For<IIntegrationEventExecutionRepository>();
        var handler = new IntegrationEventHandler(logger, mediator, integrationEventExecutionRepository);
        var eventEnvelope = new IntegrationEventEnvelope
        {
            EventId = "",
            EventType = "Mock/MockPartiallySuccessfulEvent",
            EventPayload = JsonSerializer.Serialize(new MockPartiallySuccessfulEvent()),
        };
        var eventExecution = new IntegrationEventExecution
        {
            Id = "",
            EventId = "",
            Status = default,
            StartedOn = default,
            TypeName = "",
            TypeNamespace = "",
            Payload = "",
            SourceCommandId = null,
        };
        integrationEventExecutionRepository
            .AddItemAsync(Arg.Any<IntegrationEventExecution>(), Arg.Any<CancellationToken>())
            .Returns(eventExecution);

        // Act
        var handled = await handler.TryHandleEventAsync(eventEnvelope, CancellationToken.None);

        // Assert
        Assert.Equal(IntegrationEventExecution.ExecutionStatus.PartiallySuccessful, eventExecution.Status);
        Assert.True(handled);
    }

    [Fact]
    public async Task TryHandleEventAsync_Failed_status_counts_as_NOT_handled()
    {
        // Arrange
        var logger = Substitute.For<ILogger<IntegrationEventHandler>>();
        var serviceProvider = new ServiceCollection().AddMediatR(c => c.RegisterServicesFromAssemblies(GetType().Assembly)).BuildServiceProvider();
        var mediator = new Mediator(serviceProvider);
        var integrationEventExecutionRepository = Substitute.For<IIntegrationEventExecutionRepository>();
        var handler = new IntegrationEventHandler(logger, mediator, integrationEventExecutionRepository);
        var eventEnvelope = new IntegrationEventEnvelope
        {
            EventId = "",
            EventType = "Mock/MockFailedEvent",
            EventPayload = JsonSerializer.Serialize(new MockFailedEvent()),
        };
        var eventExecution = new IntegrationEventExecution
        {
            Id = "",
            EventId = "",
            Status = default,
            StartedOn = default,
            TypeName = "",
            TypeNamespace = "",
            Payload = "",
            SourceCommandId = null,
        };
        integrationEventExecutionRepository
            .AddItemAsync(Arg.Any<IntegrationEventExecution>(), Arg.Any<CancellationToken>())
            .Returns(eventExecution);

        // Act
        var handled = await handler.TryHandleEventAsync(eventEnvelope, CancellationToken.None);

        // Assert
        Assert.Equal(IntegrationEventExecution.ExecutionStatus.Failed, eventExecution.Status);
        Assert.False(handled);
    }

    [Fact]
    public void No_duplicate_IncomingIntegrationEvent_types()
    {
        // Arrange

        // Act
        _testOutputHelper.WriteLine($"Incoming event types = {string.Join(", ", IncomingIntegrationEventTypeStringsOnly)}");
        var duplicates = IncomingIntegrationEventTypeStringsOnly
            .GroupBy(eventType => eventType)
            .Where(group => group.Count() > 1)
            .ToList();

        // Assert
        foreach (var duplicate in duplicates)
        {
            _testOutputHelper.WriteLine($"Duplicate incoming event type found: {duplicate.Key}");
        }

        Assert.Empty(duplicates);
    }

    [Fact]
    public void Event_of_ICommandsFactory_has_Incoming_attribute()
    {
        // Arrange
        var allTypesHandledByCommandsFactory = ReflectionHelpers.GetAllLoadableTypes()
            .Where(type => type.GetInterfaces().Any(interfaceType =>
                interfaceType.IsGenericType
                && interfaceType.GetGenericTypeDefinition() == typeof(ICommandsFactory<>)))
            .Select(type => type.GetInterfaces()
                .Single(interfaceType =>
                    interfaceType.IsGenericType
                    && interfaceType.GetGenericTypeDefinition() == typeof(ICommandsFactory<>))
                .GenericTypeArguments.First())
            .ToList();
        Assert.NotEmpty(allTypesHandledByCommandsFactory);

        // Act
        var typesWithoutAttribute = allTypesHandledByCommandsFactory
            .Select(type => new
            {
                Type = type,
                IncomingIntegrationEventTypeAttribute = (IncomingIntegrationEventTypeAttribute?)type.GetCustomAttribute(typeof(IncomingIntegrationEventTypeAttribute)),
            })
            .Where(x => x.IncomingIntegrationEventTypeAttribute == null)
            .ToList();

        // Assert
        if (typesWithoutAttribute.Any())
        {
            var combined = string.Join(". ", typesWithoutAttribute.Select(x => x.Type.FullName));
            throw new Exception(
                "The following IntegrationEvent types have a ICommandsFactory but still needs " +
                $"a {nameof(IncomingIntegrationEventTypeAttribute)}, please add an attribute on them: {combined}");
        }
    }

    [Fact] public void IncomingIntegrationEventTypes_enumerable_is_not_empty() => Assert.NotEmpty(IncomingIntegrationEventTypes);
    [Fact] public void IncomingIntegrationEventTypeStringsOnly_enumerable_is_not_empty() => Assert.NotEmpty(IncomingIntegrationEventTypeStringsOnly);
    [Fact] public void IncomingIntegrationEventTypeStringsWithPayloadInstancesMemberData_enumerable_is_not_empty() => Assert.NotEmpty(IncomingIntegrationEventTypeStringsWithPayloadInstancesMemberData);

    public static IEnumerable<object[]> IncomingIntegrationEventTypes =>
        IntegrationEventHandlingAssertionHelpers.GetAllIncomingIntegrationEventTypes()
            .Where(type => type != typeof(MockPartiallySuccessfulEvent))
            .Select(type => new object[] { type });

    private static IEnumerable<string> IncomingIntegrationEventTypeStringsOnly =>
        IntegrationEventsTestHelper.CreateEmptyNewIncomingIntegrationEvents().Select(x => x.EventTypeString);

    public static IEnumerable<object[]> IncomingIntegrationEventTypeStringsWithPayloadInstancesMemberData =>
        IntegrationEventsTestHelper.CreateEmptyNewIncomingIntegrationEvents().Select(x => new[] { x.EventTypeString, x.EmptyEventPayload });

    [IncomingIntegrationEventType("Mock/MockSuccessfulEvent")]
    private class MockSuccessfulEvent
    {
        // ReSharper disable once UnusedType.Local
        public class CommandsFactory : ICommandsFactory<MockSuccessfulEvent>
        {
            public async Task<IEnumerable<ICommandRequest>> Handle(
                CreateCommandsFromIntegrationEventRequest<MockSuccessfulEvent> request,
                CancellationToken cancellationToken)
            {
                await Task.CompletedTask;
                return new ICommandRequest[]
                {
                    new MockSuccessfulCommand(),
                    new MockSuccessfulCommand(),
                };
            }
        }
    }

    [IncomingIntegrationEventType("Mock/MockPartiallySuccessfulEvent")]
    private class MockPartiallySuccessfulEvent
    {
        // ReSharper disable once UnusedType.Local
        public class CommandsFactory : ICommandsFactory<MockPartiallySuccessfulEvent>
        {
            public async Task<IEnumerable<ICommandRequest>> Handle(
                CreateCommandsFromIntegrationEventRequest<MockPartiallySuccessfulEvent> request,
                CancellationToken cancellationToken)
            {
                await Task.CompletedTask;
                return new ICommandRequest[]
                {
                    new MockSuccessfulCommand(),
                    new MockExceptionFailCommand(),
                };
            }
        }
    }

    [IncomingIntegrationEventType("Mock/MockFailedEvent")]
    private class MockFailedEvent
    {
        // ReSharper disable once UnusedType.Local
        public class CommandsFactory : ICommandsFactory<MockFailedEvent>
        {
            public async Task<IEnumerable<ICommandRequest>> Handle(
                CreateCommandsFromIntegrationEventRequest<MockFailedEvent> request,
                CancellationToken cancellationToken)
            {
                await Task.CompletedTask;
                return new ICommandRequest[]
                {
                    new MockExceptionFailCommand(),
                    new MockExceptionFailCommand(),
                };
            }
        }
    }

    internal class MockSuccessfulCommand : BaseCommand
    {
        // ReSharper disable once UnusedType.Global
        public class CommandHandler : IRequestHandler<MockSuccessfulCommand>
        {
            public Task Handle(MockSuccessfulCommand request, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }
    }

    internal class MockExceptionFailCommand : BaseCommand
    {
        // ReSharper disable once UnusedType.Global
        public class CommandHandler : IRequestHandler<MockExceptionFailCommand>
        {
            public Task Handle(MockExceptionFailCommand request, CancellationToken cancellationToken)
            {
                throw new MockCommandException();
            }
        }
    }

    internal class MockCommandException : Exception
    {
    }
}