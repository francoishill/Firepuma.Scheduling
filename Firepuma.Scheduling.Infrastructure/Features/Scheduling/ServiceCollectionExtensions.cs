using Firepuma.DatabaseRepositories.CosmosDb;
using Firepuma.Scheduling.Domain.Features.Scheduling.Entities;
using Firepuma.Scheduling.Domain.Features.Scheduling.Repositories;
using Firepuma.Scheduling.Domain.Features.Scheduling.Services;
using Firepuma.Scheduling.Infrastructure.Features.Scheduling.Repositories;
using Firepuma.Scheduling.Infrastructure.Features.Scheduling.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Firepuma.Scheduling.Infrastructure.Features.Scheduling;

public static class ServiceCollectionExtensions
{
    public static void AddSchedulingFeature(
        this IServiceCollection services,
        string scheduledJobsContainerId)
    {
        if (scheduledJobsContainerId == null) throw new ArgumentNullException(nameof(scheduledJobsContainerId));

        services.AddCosmosDbRepository<
            ScheduledJob,
            IScheduledJobRepository,
            ScheduledJobCosmosDbRepository>(
            scheduledJobsContainerId,
            (
                logger,
                container,
                _) => new ScheduledJobCosmosDbRepository(logger, container));

        services.AddSingleton<ICronCalculator, CronCalculator>();
    }
}