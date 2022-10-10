using Firepuma.CommandsAndQueries.Abstractions.Entities;
using Firepuma.DatabaseRepositories.CosmosDb.Services.Requests;
using Firepuma.Scheduling.Domain.Features.Scheduling.Entities;
using Microsoft.Azure.Cosmos;

namespace Firepuma.Scheduling.FunctionApp.Configuration;

public static class CosmosContainerConfiguration
{
    public static readonly ContainerSpecification AuthorizationFailures = new()
    {
        ContainerProperties = new ContainerProperties(id: "AuthorizationFailures", partitionKeyPath: $"/{nameof(AuthorizationFailureEvent.PartitionKey)}"),
    };

    public static readonly ContainerSpecification CommandExecutions = new()
    {
        ContainerProperties = new ContainerProperties(id: "CommandExecutions", partitionKeyPath: $"/{nameof(CommandExecutionEvent.PartitionKey)}"),
    };

    public static readonly ContainerSpecification ScheduledJobs = new()
    {
        ContainerProperties = new ContainerProperties(id: "ScheduledJobs", partitionKeyPath: $"/{nameof(ScheduledJob.ApplicationId)}"),
    };

    public static readonly ContainerSpecification[] AllContainers =
    {
        AuthorizationFailures,
        CommandExecutions,
        ScheduledJobs,
    };
}