using Firepuma.DatabaseRepositories.MongoDb.Repositories;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Entities;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Repositories;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Firepuma.Scheduling.Infrastructure.Plumbing.IntegrationEvents.Repositories;

public class IntegrationEventExecutionMongoDbRepository : MongoDbRepository<IntegrationEventExecution>, IIntegrationEventExecutionRepository
{
    public IntegrationEventExecutionMongoDbRepository(ILogger logger, IMongoCollection<IntegrationEventExecution> collection)
        : base(logger, collection)
    {
    }
}