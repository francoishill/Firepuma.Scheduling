// ReSharper disable InconsistentNaming

using Firepuma.CommandsAndQueries.Abstractions.Entities;
using Firepuma.Scheduling.FunctionApp.Features.Scheduling.Entities;
using Microsoft.Azure.Cosmos;

namespace Firepuma.Scheduling.FunctionApp.Config;

public static class CosmosContainersConfig
{
    public static readonly ContainerProperties ScheduledJobs = new(id: "ScheduledJobs", partitionKeyPath: $"/{nameof(ScheduledJob.ApplicationId)}");
    public static readonly ContainerProperties CommandExecutions = new(id: "CommandExecutions", partitionKeyPath: $"/{nameof(CommandExecutionEvent.PartitionKey)}");
}