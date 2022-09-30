// ReSharper disable InconsistentNaming

namespace Firepuma.Scheduling.FunctionApp.Infrastructure.CosmosDb;

public static class CosmosContainers
{
    public static readonly ContainerConfig ScheduledJobs = new ContainerConfig("ScheduledJobs", "/ApplicationId");

    public class ContainerConfig
    {
        public string ContainerName { get; set; }
        public string PartitionKeyPath { get; set; }

        public ContainerConfig(
            string containerName,
            string partitionKeyPath)
        {
            ContainerName = containerName;
            PartitionKeyPath = partitionKeyPath;
        }
    }
}