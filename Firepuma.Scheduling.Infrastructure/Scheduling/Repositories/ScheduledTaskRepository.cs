using Firepuma.DatabaseRepositories.MongoDb.Repositories;
using Firepuma.Scheduling.Domain.Entities;
using Firepuma.Scheduling.Domain.Repositories;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Firepuma.Scheduling.Infrastructure.Scheduling.Repositories;

internal class ScheduledTaskRepository : MongoDbRepository<ScheduledTask>, IScheduledTaskRepository
{
    public ScheduledTaskRepository(ILogger logger, IMongoCollection<ScheduledTask> collection)
        : base(logger, collection)
    {
    }
}