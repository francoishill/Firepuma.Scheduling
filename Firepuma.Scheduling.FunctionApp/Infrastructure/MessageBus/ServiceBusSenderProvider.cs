using System.Collections.Generic;
using Azure.Messaging.ServiceBus;
using Firepuma.Scheduling.FunctionApp.Abstractions.ClientApplications.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Firepuma.Scheduling.FunctionApp.Infrastructure.MessageBus;

public class ServiceBusSenderProvider
{
    private readonly ILogger<ServiceBusSenderProvider> _logger;
    private readonly Dictionary<ClientApplicationId, ServiceBusSender> _sendersMap;

    public ServiceBusSenderProvider(
        ILogger<ServiceBusSenderProvider> logger,
        Dictionary<ClientApplicationId, ServiceBusSender> sendersMap)
    {
        _logger = logger;
        _sendersMap = sendersMap;
    }

    public bool TryGetSender(ClientApplicationId applicationId, out ServiceBusSender sender)
    {
        if (!_sendersMap.TryGetValue(applicationId, out sender))
        {
            _logger.LogWarning("Unable to find service bus sender for applicationId '{AppId}', available Ids are: {Ids}", applicationId, string.Join(", ", _sendersMap.Keys));
            return false;
        }

        return true;
    }
}