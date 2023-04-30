using Firepuma.DatabaseRepositories.Abstractions.Repositories;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Entities;

namespace Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Repositories;

public interface IIntegrationEventExecutionRepository : IRepository<IntegrationEventExecution>
{
}