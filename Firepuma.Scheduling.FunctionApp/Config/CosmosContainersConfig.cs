// ReSharper disable InconsistentNaming

using Microsoft.Azure.Cosmos;

namespace Firepuma.Scheduling.FunctionApp.Config;

public static class CosmosContainersConfig
{
    public static readonly ContainerProperties ScheduledJobs = new(id: "ScheduledJobs", partitionKeyPath: "/ApplicationId");
}