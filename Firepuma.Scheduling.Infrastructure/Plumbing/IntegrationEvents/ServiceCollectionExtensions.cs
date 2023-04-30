using Firepuma.BusMessaging.GooglePubSub;
using Firepuma.DatabaseRepositories.MongoDb;
using Firepuma.EventMediation.IntegrationEvents.Helpers;
using Firepuma.Scheduling.Domain.Plumbing.CommandHandling.Services;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Attributes;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Config;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Entities;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Repositories;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Services;
using Firepuma.Scheduling.Domain.Plumbing.Reflection;
using Firepuma.Scheduling.Infrastructure.Plumbing.IntegrationEvents.Assertions;
using Firepuma.Scheduling.Infrastructure.Plumbing.IntegrationEvents.Repositories;
using Firepuma.Scheduling.Infrastructure.Plumbing.IntegrationEvents.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Firepuma.Scheduling.Infrastructure.Plumbing.IntegrationEvents;

public static class ServiceCollectionExtensions
{
    public static void AddIntegrationEvents(
        this IServiceCollection services,
        IConfigurationSection integrationEventsConfigSection,
        bool isDevelopmentEnvironment,
        string integrationEventExecutionsCollectionName)
    {
        if (integrationEventsConfigSection == null) throw new ArgumentNullException(nameof(integrationEventsConfigSection));
        if (integrationEventExecutionsCollectionName == null) throw new ArgumentNullException(nameof(integrationEventExecutionsCollectionName));

        services.AddOptions<IntegrationEventsOptions>().Bind(integrationEventsConfigSection).ValidateDataAnnotations().ValidateOnStart();

        services.AddGooglePubSubPublisherClientCache();
        services.AddGooglePubSubMessageParser();

        services.AddTransient<ICommandContext, CommandContext>();
        services.AddTransient<ICommandEventPublisher, CommandEventPublisher>();
        services.AddTransient<IIntegrationEventTransport, GooglePubSubIntegrationEventTransport>();
        services.AddTransient<IIntegrationEventHandler, IntegrationEventHandler>();

        services.AddMemoryCache();

        services.AddMongoDbRepository<
            IntegrationEventExecution,
            IIntegrationEventExecutionRepository,
            IntegrationEventExecutionMongoDbRepository>(
            integrationEventExecutionsCollectionName,
            (logger, collection, _) => new IntegrationEventExecutionMongoDbRepository(logger, collection),
            indexesFactory: IntegrationEventExecution.GetSchemaIndexes);

        if (isDevelopmentEnvironment)
        {
            AssertNoDuplicateIncomingIntegrationEventTypeAttributes();
            AssertNoDuplicateOutgoingIntegrationEventTypeAttributes();
            AssertExactlyOneHandlerPerIncomingIntegrationEvent();
        }
    }

    private static void AssertNoDuplicateIncomingIntegrationEventTypeAttributes()
    {
        var duplicateIntegrationEventTypes = EventTypeHelpers.GetDuplicateIntegrationEventTypeAttributes<IncomingIntegrationEventTypeAttribute>(
                ReflectionHelpers.GetAllLoadableTypes(),
                attribute => attribute.IntegrationEventType)
            .ToList();

        if (duplicateIntegrationEventTypes.Any())
        {
            throw new Exception($"Duplicate integration event type attributes were found: {string.Join(", ", duplicateIntegrationEventTypes)}");
        }
    }

    private static void AssertNoDuplicateOutgoingIntegrationEventTypeAttributes()
    {
        var duplicateIntegrationEventTypes = EventTypeHelpers.GetDuplicateIntegrationEventTypeAttributes<OutgoingIntegrationEventTypeAttribute>(
                ReflectionHelpers.GetAllLoadableTypes(),
                attribute => attribute.IntegrationEventType)
            .ToList();

        if (duplicateIntegrationEventTypes.Any())
        {
            throw new Exception($"Duplicate integration event type attributes were found: {string.Join(", ", duplicateIntegrationEventTypes)}");
        }
    }

    private static void AssertExactlyOneHandlerPerIncomingIntegrationEvent()
    {
        var eventsWithHandlers = IntegrationEventHandlingAssertionHelpers.GetAllIncomingIntegrationEventTypes()
            .Select(eventType => new
            {
                integrationEvent = eventType,
                handlers = ReflectionHelpers.GetRegisteredHandlersForRequestType(eventType).ToList(),
            })
            .ToList();

        var eventsWithDuplicateHandlers = eventsWithHandlers.Where(x => x.handlers.Count > 1).ToList();
        if (eventsWithDuplicateHandlers.Any())
        {
            var combined = string.Join(", ", eventsWithDuplicateHandlers
                .Select(x => $"{x.integrationEvent.FullName ?? x.integrationEvent.Name} has handlers: {string.Join(", ", x.handlers.Select(h => h.FullName ?? h.Name))}"));

            throw new Exception($"Duplicate integration event handlers: {combined}");
        }

        var eventsWithNoHandlers = eventsWithHandlers.Where(x => x.handlers.Count == 0).ToList();
        if (eventsWithNoHandlers.Any())
        {
            var combined = string.Join(", ", eventsWithNoHandlers.Select(x => x.integrationEvent.FullName ?? x.integrationEvent.Name));
            throw new Exception($"No handlers for these integration events (please register a handler or add to the ignored list in `IntegrationEventHandlingAssertionHelpers.GetAllIntegrationEventTypes`): {combined}");
        }
    }
}