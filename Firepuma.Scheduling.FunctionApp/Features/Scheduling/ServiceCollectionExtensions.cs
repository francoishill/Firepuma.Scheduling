using Firepuma.DatabaseRepositories.CosmosDb;
using Firepuma.Scheduling.FunctionApp.Config;
using Firepuma.Scheduling.FunctionApp.Features.Scheduling.Entities;
using Firepuma.Scheduling.FunctionApp.Features.Scheduling.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Firepuma.Scheduling.FunctionApp.Features.Scheduling;

public static class ServiceCollectionExtensions
{
    public static void AddSchedulingFeature(this IServiceCollection services)
    {
        services.AddCosmosDbRepository<
            ScheduledJob,
            IScheduledJobRepository,
            ScheduledJobCosmosDbRepository>(
            CosmosContainersConfig.ScheduledJobs.Id,
            (logger, container) => new ScheduledJobCosmosDbRepository(logger, container));
    }
}