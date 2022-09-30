using Firepuma.Scheduling.FunctionApp.Features.Scheduling.Entities;
using Firepuma.Scheduling.FunctionApp.Features.Scheduling.Repositories;
using Firepuma.Scheduling.FunctionApp.Infrastructure.CosmosDb;
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
            CosmosContainers.ScheduledJobs.ContainerName,
            (logger, container) => new ScheduledJobCosmosDbRepository(logger, container));
    }
}