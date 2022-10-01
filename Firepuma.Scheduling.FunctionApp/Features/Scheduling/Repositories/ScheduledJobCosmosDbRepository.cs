using System;
using Firepuma.DatabaseRepositories.CosmosDb.Repositories;
using Firepuma.Scheduling.FunctionApp.Features.Scheduling.Entities;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

// ReSharper disable SuggestBaseTypeForParameterInConstructor

namespace Firepuma.Scheduling.FunctionApp.Features.Scheduling.Repositories;

public class ScheduledJobCosmosDbRepository : CosmosDbRepository<ScheduledJob>, IScheduledJobRepository
{
    public ScheduledJobCosmosDbRepository(
        ILogger<ScheduledJobCosmosDbRepository> logger,
        Container container)
        : base(logger, container)
    {
    }

    protected override string GenerateId(ScheduledJob entity) => $"{Guid.NewGuid().ToString()}:{entity.ApplicationId}";
    protected override PartitionKey ResolvePartitionKey(string entityId) => new(entityId.Split(':')[1]);
}