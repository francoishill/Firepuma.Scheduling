using Firepuma.DatabaseRepositories.Abstractions.Repositories;
using Firepuma.Scheduling.Domain.Entities;

namespace Firepuma.Scheduling.Domain.Repositories;

public interface IScheduledTaskRepository : IRepository<ScheduledTask>
{
}