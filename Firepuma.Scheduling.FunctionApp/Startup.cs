using System.Collections.Generic;
using System.Linq;
using Azure.Messaging.ServiceBus;
using Firepuma.DatabaseRepositories.CosmosDb;
using Firepuma.Scheduling.FunctionApp;
using Firepuma.Scheduling.FunctionApp.Abstractions.ClientApplications.ValueObjects;
using Firepuma.Scheduling.FunctionApp.Config;
using Firepuma.Scheduling.FunctionApp.Features.Scheduling;
using Firepuma.Scheduling.FunctionApp.Infrastructure.MessageBus;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

        services.AddSingleton<ServiceBusSenderProvider>(s =>
        {
            var clientAppConfigs = s.GetRequiredService<IOptions<ClientApplicationConfigs>>().Value.Values;

            var logger = s.GetRequiredService<ILogger<ServiceBusSenderProvider>>();

            var client = s.GetRequiredService<ServiceBusClient>();
            var sendersMap = clientAppConfigs.ToDictionary(
                x => new ClientApplicationId(x.ApplicationId),
                x => client.CreateSender(x.QueueName));

            return new ServiceBusSenderProvider(logger, sendersMap);
        });

        var cosmosConnectionString = config.GetValue<string>("FirepumaScheduling:CosmosConnectionString");
        var cosmosDatabaseId = config.GetValue<string>("FirepumaScheduling:CosmosDatabaseId");
        services.AddCosmosDbRepositories(options =>
            {
                options.ConnectionString = cosmosConnectionString;
                options.DatabaseId = cosmosDatabaseId;
            },
            validateOnStart: false);

        services.AddSchedulingFeature();
    }
}