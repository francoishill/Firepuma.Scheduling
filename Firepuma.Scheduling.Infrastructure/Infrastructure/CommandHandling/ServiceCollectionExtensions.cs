using System.Reflection;
using Firepuma.CommandsAndQueries.CosmosDb;
using Firepuma.CommandsAndQueries.CosmosDb.Config;
using Firepuma.Scheduling.Infrastructure.Infrastructure.CommandHandling.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Firepuma.Scheduling.Infrastructure.Infrastructure.CommandHandling;

public static class ServiceCollectionExtensions
{
    public static void AddCommandsAndQueriesFunctionalityForFunction(
        this IServiceCollection services,
        string authorizationFailuresContainerId,
        string commandExecutionsContainerId,
        Assembly[] assembliesWithCommandHandlers)
    {
        if (authorizationFailuresContainerId == null) throw new ArgumentNullException(nameof(authorizationFailuresContainerId));
        if (commandExecutionsContainerId == null) throw new ArgumentNullException(nameof(commandExecutionsContainerId));

        assembliesWithCommandHandlers = assembliesWithCommandHandlers.Distinct().ToArray();

        services
            .AddCommandHandlingWithCosmosDbStorage(
                new CosmosDbCommandHandlingOptions
                {
                    AddWrapCommandExceptionsPipelineBehavior = true,
                    AddLoggingScopePipelineBehavior = true,
                    AddPerformanceLoggingPipelineBehavior = true,

                    AddValidationBehaviorPipeline = true,
                    ValidationHandlerMarkerAssemblies = assembliesWithCommandHandlers,

                    AddAuthorizationBehaviorPipeline = true,
                    AuthorizationFailurePartitionKeyGenerator = typeof(AuthorizationFailurePartitionKeyGenerator),
                    AuthorizationFailureEventContainerName = authorizationFailuresContainerId,
                    AuthorizationHandlerMarkerAssemblies = assembliesWithCommandHandlers,

                    AddRecordingOfExecution = true,
                    CommandExecutionPartitionKeyGenerator = typeof(CommandExecutionPartitionKeyGenerator),
                    CommandExecutionEventContainerName = commandExecutionsContainerId,
                });

        services.AddMediatR(assembliesWithCommandHandlers);
    }
}