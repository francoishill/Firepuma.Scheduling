using Firepuma.DatabaseRepositories.Abstractions.Repositories;
using Firepuma.Scheduling.Domain.Features.Scheduling.Entities;

namespace Firepuma.Scheduling.Domain.Features.Scheduling.Repositories;

public interface IScheduledJobRepository : IRepository<ScheduledJob>
{
}