using Firepuma.Scheduling.FunctionApp.Abstractions.Repositories;
using Firepuma.Scheduling.FunctionApp.Features.Scheduling.Entities;

namespace Firepuma.Scheduling.FunctionApp.Features.Scheduling.Repositories;

public interface IScheduledJobRepository : IRepository<ScheduledJob>
{
}