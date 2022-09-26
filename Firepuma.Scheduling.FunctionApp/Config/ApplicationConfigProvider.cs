using Microsoft.Extensions.Logging;

namespace Firepuma.Scheduling.FunctionApp.Config;

public class ApplicationConfigProvider
{
    private readonly ILogger<ApplicationConfigProvider> _logger;

    public ApplicationConfigProvider(
        ILogger<ApplicationConfigProvider> logger)
    {
        _logger = logger;

        _logger.LogInformation("ApplicationConfigProvider class created");
    }
}