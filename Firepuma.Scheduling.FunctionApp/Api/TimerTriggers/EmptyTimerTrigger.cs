using System;
using System.Threading.Tasks;
using Firepuma.Scheduling.FunctionApp.Config;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Firepuma.Scheduling.FunctionApp.Api.TimerTriggers;

public class EmptyTimerTrigger
{
    private readonly ApplicationConfigProvider _applicationConfigProvider;

    public EmptyTimerTrigger(
        ApplicationConfigProvider applicationConfigProvider)
    {
        _applicationConfigProvider = applicationConfigProvider;
    }

    [FunctionName("EmptyTimerTrigger")]
    public async Task RunAsync([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer, ILogger log)
    {
        log.LogInformation("C# Timer trigger function executed at: {Time}", DateTime.UtcNow);
    }
}