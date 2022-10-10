using Azure.Messaging.ServiceBus;
using Firepuma.Scheduling.Domain.Features.Scheduling.ValueObjects;
using Firepuma.Scheduling.Domain.Infrastructure.MessageBus;
using Firepuma.Scheduling.Infrastructure.Infrastructure.MessageBus.Sender;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// ReSharper disable RedundantTypeArgumentsOfMethod

namespace Firepuma.Scheduling.Infrastructure.Infrastructure.MessageBus;

public static class ServiceCollectionExtensions
{
    public static void AddClientAppBusMessageSenders(
        this IServiceCollection services,
        Func<IServiceProvider, ClientApplicationConfigs> clientApplicationConfigsFactory)
    {
        services.AddSingleton<IClientAppBusMessageSender>(s =>
        {
            var clientAppConfigs = clientApplicationConfigsFactory(s).Values;

            var logger = s.GetRequiredService<ILogger<ClientAppBusMessageSender>>();

            var client = s.GetRequiredService<ServiceBusClient>();
            var sendersMap = clientAppConfigs.ToDictionary(
                x => new ClientApplicationId(x.ApplicationId),
                x => client.CreateSender(x.QueueName));

            return new ClientAppBusMessageSender(logger, sendersMap);
        });
    }
}