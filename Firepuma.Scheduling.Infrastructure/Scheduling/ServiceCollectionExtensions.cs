using Firepuma.DatabaseRepositories.MongoDb;
using Firepuma.Scheduling.Domain.Entities;
using Firepuma.Scheduling.Domain.Repositories;
using Firepuma.Scheduling.Domain.Services;
using Firepuma.Scheduling.Infrastructure.Scheduling.Repositories;
using Firepuma.Scheduling.Infrastructure.Scheduling.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Firepuma.Scheduling.Infrastructure.Scheduling;

public static class ServiceCollectionExtensions
{
    public static void AddSchedulingFeature(
        this IServiceCollection services,
        string scheduledTasksCollectionName)
    {
        if (scheduledTasksCollectionName == null) throw new ArgumentNullException(nameof(scheduledTasksCollectionName));

        services.AddTransient<ICronCalculator, CronCalculator>();

        services.AddMongoDbRepository<
            ScheduledTask,
            IScheduledTaskRepository,
            ScheduledTaskRepository>(
            scheduledTasksCollectionName,
            (logger, collection, _) => new ScheduledTaskRepository(logger, collection),
            indexesFactory: ScheduledTask.GetSchemaIndexes);
    }
}