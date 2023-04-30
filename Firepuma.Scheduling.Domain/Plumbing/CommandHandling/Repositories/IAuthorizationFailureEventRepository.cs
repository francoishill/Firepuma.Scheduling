using Firepuma.DatabaseRepositories.Abstractions.Repositories;
using Firepuma.Scheduling.Domain.Plumbing.CommandHandling.Entities;

namespace Firepuma.Scheduling.Domain.Plumbing.CommandHandling.Repositories;

public interface IAuthorizationFailureEventRepository : IRepository<AuthorizationFailureMongoDbEvent>
{
}