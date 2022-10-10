using Firepuma.DatabaseRepositories.CosmosDb.Repositories;
using Firepuma.Scheduling.Domain.Features.Scheduling.Entities;
using Firepuma.Scheduling.Domain.Features.Scheduling.Repositories;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

// ReSharper disable SuggestBaseTypeForParameterInConstructor

namespace Firepuma.Scheduling.Infrastructure.Features.Scheduling.Repositories;

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