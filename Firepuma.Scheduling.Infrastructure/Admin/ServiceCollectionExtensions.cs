using Firepuma.Scheduling.Domain.Admin.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Firepuma.Scheduling.Infrastructure.Admin;

public static class ServiceCollectionExtensions
{
    public static void AddAdminFeature(
        this IServiceCollection services)
    {
        services.AddTransient<ICommandExecutionQueryService, CommandExecutionMongoDbQueryService>();
    }
}