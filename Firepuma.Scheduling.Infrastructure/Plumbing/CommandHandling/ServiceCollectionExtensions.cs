using System.Reflection;
using Firepuma.CommandsAndQueries.Abstractions;
using Firepuma.CommandsAndQueries.Abstractions.PipelineBehaviors;
using Firepuma.DatabaseRepositories.MongoDb;
using Firepuma.Scheduling.Domain.Plumbing.CommandHandling.Entities;
using Firepuma.Scheduling.Domain.Plumbing.CommandHandling.PipelineBehaviors;
using Firepuma.Scheduling.Domain.Plumbing.CommandHandling.Repositories;
using Firepuma.Scheduling.Domain.Plumbing.Reflection;
using Firepuma.Scheduling.Infrastructure.Plumbing.CommandHandling.Assertions;
using Firepuma.Scheduling.Infrastructure.Plumbing.CommandHandling.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Firepuma.Scheduling.Infrastructure.Plumbing.CommandHandling;

public static class ServiceCollectionExtensions
{
    public static void AddCommandsAndQueriesFunctionality(
        this IServiceCollection services,
        bool isDevelopmentEnvironment,
        string authorizationFailuresCollectionName,
        string commandExecutionsCollectionName,
        Assembly[] assembliesWithCommandHandlers)
    {
        if (authorizationFailuresCollectionName == null) throw new ArgumentNullException(nameof(authorizationFailuresCollectionName));
        if (commandExecutionsCollectionName == null) throw new ArgumentNullException(nameof(commandExecutionsCollectionName));

        assembliesWithCommandHandlers = assembliesWithCommandHandlers.Distinct().ToArray();

        if (assembliesWithCommandHandlers.Length == 0)
        {
            throw new ArgumentException($"At least one assembly is required for {nameof(assembliesWithCommandHandlers)}", nameof(assembliesWithCommandHandlers));
        }

        services.AddMongoDbRepository<
            CommandExecutionMongoDbEvent,
            ICommandExecutionRepository,
            CommandExecutionMongoDbRepository>(
            commandExecutionsCollectionName,
            (logger, collection, _) => new CommandExecutionMongoDbRepository(logger, collection),
            indexesFactory: CommandExecutionMongoDbEvent.GetSchemaIndexes);

        services.AddMongoDbRepository<
            AuthorizationFailureMongoDbEvent,
            IAuthorizationFailureEventRepository,
            AuthorizationFailureEventMongoDbRepository>(
            authorizationFailuresCollectionName,
            (logger, collection, _) => new AuthorizationFailureEventMongoDbRepository(logger, collection),
            indexesFactory: AuthorizationFailureMongoDbEvent.GetSchemaIndexes);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(WrapCommandExceptionPipeline<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingScopePipeline<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceLogPipeline<,>));

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PrerequisitesPipelineBehavior<,>));
        services.AddValidatorsFromAssemblies(assembliesWithCommandHandlers, ServiceLifetime.Transient);
        services.AddAuthorizersFromAssemblies(assembliesWithCommandHandlers);

        // Add this after prerequisites because we don't want to record command execution events for validation/authorization failures
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CommandExecutionRecordingPipeline<,>));

        if (isDevelopmentEnvironment)
        {
            AssertExactlyOneHandlerPerCommand();
            AssertExactlyOneHandlerPerQuery();
            // AssertExactlyOneHandlerPerAuthorizationRequirement();
        }
    }

    private static void AssertExactlyOneHandlerPerCommand()
    {
        var commandsWithHandlers = CommandHandlingAssertionHelpers.GetAllCommandTypes()
            .Select(commandType => new
            {
                command = commandType,
                handlers = ReflectionHelpers.GetRegisteredHandlersForRequestType(commandType).ToList(),
            })
            .ToList();

        var commandsWithDuplicateHandlers = commandsWithHandlers.Where(x => x.handlers.Count > 1).ToList();
        if (commandsWithDuplicateHandlers.Any())
        {
            var combined = string.Join(", ", commandsWithDuplicateHandlers
                .Select(x => $"{x.command.FullName ?? x.command.Name} has handlers: {string.Join(", ", x.handlers.Select(h => h.FullName ?? h.Name))}"));

            throw new Exception($"Duplicate command handlers: {combined}");
        }

        var commandsWithNoHandlers = commandsWithHandlers.Where(x => x.handlers.Count == 0).ToList();
        if (commandsWithNoHandlers.Any())
        {
            var combined = string.Join(", ", commandsWithNoHandlers.Select(x => x.command.FullName ?? x.command.Name));
            throw new Exception($"No handlers for these commands: {combined}");
        }
    }

    private static void AssertExactlyOneHandlerPerQuery()
    {
        var queriesWithHandlers = CommandHandlingAssertionHelpers.GetAllQueryTypes()
            .Select(queryType => new
            {
                query = queryType,
                handlers = ReflectionHelpers.GetRegisteredHandlersForRequestType(queryType).ToList(),
            })
            .ToList();

        var queriesWithDuplicateHandlers = queriesWithHandlers.Where(x => x.handlers.Count > 1).ToList();
        if (queriesWithDuplicateHandlers.Any())
        {
            var combined = string.Join(", ", queriesWithDuplicateHandlers
                .Select(x => $"{x.query.FullName ?? x.query.Name} has handlers: {string.Join(", ", x.handlers.Select(h => h.FullName ?? h.Name))}"));

            throw new Exception($"Duplicate query handlers: {combined}");
        }

        var queriesWithNoHandlers = queriesWithHandlers.Where(x => x.handlers.Count == 0).ToList();
        if (queriesWithNoHandlers.Any())
        {
            var combined = string.Join(", ", queriesWithNoHandlers.Select(x => x.query.FullName ?? x.query.Name));
            throw new Exception($"No handlers for these queries: {combined}");
        }
    }

    // private static void AssertExactlyOneHandlerPerAuthorizationRequirement()
    // {
    //     var requirementsWithHandlers = CommandHandlingAssertionHelpers.GetAllAuthorizationRequirementTypes()
    //         .Select(requirementType => new
    //         {
    //             requirement = requirementType,
    //             handlers = ReflectionHelpers.GetRegisteredHandlersForRequestType(requirementType).ToList(),
    //         })
    //         .ToList();
    //
    //     var requirementsWithDuplicateHandlers = requirementsWithHandlers.Where(x => x.handlers.Count > 1).ToList();
    //     if (requirementsWithDuplicateHandlers.Any())
    //     {
    //         var combined = string.Join(", ", requirementsWithDuplicateHandlers
    //             .Select(x => $"{x.requirement.FullName ?? x.requirement.Name} has handlers: {string.Join(", ", x.handlers.Select(h => h.FullName ?? h.Name))}"));
    //
    //         throw new Exception($"Duplicate authorization requirement handlers: {combined}");
    //     }
    //
    //     var requirementsWithNoHandlers = requirementsWithHandlers.Where(x => x.handlers.Count == 0).ToList();
    //     if (requirementsWithNoHandlers.Any())
    //     {
    //         var combined = string.Join(", ", requirementsWithNoHandlers.Select(x => x.requirement.FullName ?? x.requirement.Name));
    //         throw new Exception($"No handlers for these authorization requirements: {combined}");
    //     }
    // }
}