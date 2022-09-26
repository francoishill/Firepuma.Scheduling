using Firepuma.Scheduling.FunctionApp;
using Firepuma.Scheduling.FunctionApp.Config;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Firepuma.Scheduling.FunctionApp;

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder.Services.AddSingleton<ApplicationConfigProvider>();
    }
}