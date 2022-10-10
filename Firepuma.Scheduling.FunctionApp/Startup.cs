using Azure.Messaging.ServiceBus;
using Firepuma.Scheduling.Domain.Features.Scheduling.Commands;
using Firepuma.Scheduling.Domain.Features.Scheduling.ValueObjects;
using Firepuma.Scheduling.FunctionApp;
using Firepuma.Scheduling.FunctionApp.Configuration;
using Firepuma.Scheduling.Infrastructure.Features.Scheduling;
using Firepuma.Scheduling.Infrastructure.Infrastructure.CommandHandling;
using Firepuma.Scheduling.Infrastructure.Infrastructure.CosmosDb;
using Firepuma.Scheduling.Infrastructure.Infrastructure.MessageBus;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

// ReSharper disable RedundantTypeArgumentsOfMethod

[assembly: FunctionsStartup(typeof(Startup))]

namespace Firepuma.Scheduling.FunctionApp;

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        var config = builder.GetContext().Configuration;
        var services = builder.Services;

        services
            .AddOptions<ClientApplicationConfigs>()
            .Bind(config.GetSection("FirepumaScheduling:ClientApps"))
            .ValidateDataAnnotations();

        var serviceBusConnectionString = config.GetValue<string>("FirepumaScheduling:ServiceBus");
        services.AddSingleton<ServiceBusClient>(_ => new ServiceBusClient(serviceBusConnectionString));

        services.AddClientAppBusMessageSenders(s => s.GetRequiredService<IOptions<ClientApplicationConfigs>>().Value);

        var cosmosConnectionString = config.GetValue<string>("FirepumaScheduling:CosmosConnectionString");
        var cosmosDatabaseId = config.GetValue<string>("FirepumaScheduling:CosmosDatabaseId");
        services.AddCosmosDbRepositoriesForFunction(cosmosConnectionString, cosmosDatabaseId);

        var authorizationFailuresContainerId = CosmosContainerConfiguration.AuthorizationFailures.ContainerProperties.Id;
        var commandExecutionsContainerId = CosmosContainerConfiguration.CommandExecutions.ContainerProperties.Id;
        var assembliesWithCommandHandlers = new[]
        {
            typeof(Startup).Assembly,
            typeof(NotifyClientOfDueJobCommand).Assembly,
            typeof(Firepuma.Scheduling.Infrastructure.Infrastructure.CosmosDb.ServiceCollectionExtensions).Assembly,
        };
        services.AddCommandsAndQueriesFunctionalityForFunction(
            authorizationFailuresContainerId,
            commandExecutionsContainerId,
            assembliesWithCommandHandlers);

        var scheduledJobsContainerId = CosmosContainerConfiguration.ScheduledJobs.ContainerProperties.Id;
        services.AddSchedulingFeature(
            scheduledJobsContainerId);
    }
}