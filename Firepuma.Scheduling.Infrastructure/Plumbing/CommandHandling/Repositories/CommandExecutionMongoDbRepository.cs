using Firepuma.DatabaseRepositories.MongoDb.Repositories;
using Firepuma.Scheduling.Domain.Plumbing.CommandHandling.Entities;
using Firepuma.Scheduling.Domain.Plumbing.CommandHandling.Repositories;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Firepuma.Scheduling.Infrastructure.Plumbing.CommandHandling.Repositories;

internal class CommandExecutionMongoDbRepository : MongoDbRepository<CommandExecutionMongoDbEvent>, ICommandExecutionRepository
{
    public CommandExecutionMongoDbRepository(
        ILogger logger,
        IMongoCollection<CommandExecutionMongoDbEvent> collection)
        : base(logger, collection)
    {
    }
}