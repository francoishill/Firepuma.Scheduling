using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Firepuma.Scheduling.FunctionApp.Infrastructure.CosmosDb;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

// ReSharper disable RedundantAnonymousTypePropertyName

namespace Firepuma.Scheduling.FunctionApp.Api.AdminHttpTriggers;

public class EnsureCosmosContainersExist
{
    private readonly Database _cosmosDb;

    public EnsureCosmosContainersExist(
        Database cosmosDb)
    {
        _cosmosDb = cosmosDb;
    }

    [FunctionName("EnsureCosmosContainersExist")]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ILogger log,
        CancellationToken cancellationToken)
    {
        log.LogInformation("C# HTTP trigger function processed a request");

        var containersToCreate = new[]
        {
            CosmosContainers.ScheduledJobs,
        };

        var successfulContainers = new List<object>();
        var failedContainers = new List<object>();
        foreach (var containerConfig in containersToCreate)
        {
            log.LogDebug(
                "Creating container {Container} with PartitionKeyPath {PartitionKeyPath}",
                containerConfig.ContainerName, containerConfig.PartitionKeyPath);

            try
            {
                await _cosmosDb.CreateContainerIfNotExistsAsync(
                    new ContainerProperties(containerConfig.ContainerName, containerConfig.PartitionKeyPath),
                    cancellationToken: cancellationToken);

                log.LogInformation(
                    "Successfully created container {Container} with PartitionKeyPath {PartitionKeyPath}",
                    containerConfig.ContainerName, containerConfig.PartitionKeyPath);

                successfulContainers.Add(new
                {
                    Container = containerConfig,
                });
            }
            catch (Exception exception)
            {
                log.LogError(
                    exception,
                    "Failed to create container {Container} with PartitionKeyPath {PartitionKeyPath}, error: {Error}, stack: {Stack}",
                    containerConfig.ContainerName, containerConfig.PartitionKeyPath,
                    exception.Message, exception.StackTrace);

                failedContainers.Add(new
                {
                    Container = containerConfig,
                    Exception = exception,
                });
            }
        }

        var responseDto = new
        {
            failedCount = failedContainers.Count,
            successfulCount = successfulContainers.Count,

            failedContainers,
            successfulContainers,
        };

        return new OkObjectResult(responseDto);
    }
}