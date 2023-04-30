using Firepuma.DatabaseRepositories.MongoDb.Repositories;
using Firepuma.Scheduling.Domain.Plumbing.CommandHandling.Entities;
using Firepuma.Scheduling.Domain.Plumbing.CommandHandling.Repositories;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Firepuma.Scheduling.Infrastructure.Plumbing.CommandHandling.Repositories;

internal class AuthorizationFailureEventMongoDbRepository : MongoDbRepository<AuthorizationFailureMongoDbEvent>, IAuthorizationFailureEventRepository
{
    public AuthorizationFailureEventMongoDbRepository(
        ILogger logger,
        IMongoCollection<AuthorizationFailureMongoDbEvent> collection)
        : base(logger, collection)
    {
    }
}