using AutoMapper;
using Firepuma.Scheduling.Domain.Commands;
using Firepuma.Scheduling.Infrastructure.Admin;
using Firepuma.Scheduling.Infrastructure.Plumbing.CommandHandling;
using Firepuma.Scheduling.Infrastructure.Plumbing.GoogleLogging;
using Firepuma.Scheduling.Infrastructure.Plumbing.IntegrationEvents;
using Firepuma.Scheduling.Infrastructure.Plumbing.MongoDb;
using Firepuma.Scheduling.Infrastructure.Plumbing.NLogLogging;
using Firepuma.Scheduling.Infrastructure.Scheduling;
using Firepuma.Scheduling.Worker.Admin;
using Firepuma.Scheduling.Worker.Plumbing.LocalDevelopment;
using Firepuma.Scheduling.Worker.Plumbing.Middleware;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddInvalidModelStateLogging();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAutoMapper(typeof(ManualHealthCheckingController));

var mongoDbConfigSection = builder.Configuration.GetSection("MongoDb");
builder.Services.AddMongoDbRepositories(mongoDbConfigSection, builder.Environment.IsDevelopment(), out var mongoDbOptions);

var assembliesWithCommandHandlers = new[]
{
    typeof(AddScheduledTask).Assembly,
}.Distinct().ToArray();

builder.Services.AddCommandsAndQueriesFunctionality(
    builder.Environment.IsDevelopment(),
    mongoDbOptions.AuthorizationFailuresCollectionName,
    mongoDbOptions.CommandExecutionsCollectionName,
    assembliesWithCommandHandlers);

var integrationEventsConfigSection = builder.Configuration.GetSection("IntegrationEvents");
builder.Services.AddIntegrationEvents(
    integrationEventsConfigSection,
    builder.Environment.IsDevelopment(),
    mongoDbOptions.IntegrationEventExecutionsCollectionName);

builder.Services.AddMediatR(c => c.RegisterServicesFromAssemblies(assembliesWithCommandHandlers));

builder.Services.AddAdminFeature();

builder.Services.AddSchedulingFeature(
    mongoDbOptions.ScheduledTasksCollectionName);

var googleLoggingConfigSection = builder.Configuration.GetSection("Logging:GoogleLogging");
builder.Logging.AddCustomGoogleLogging(googleLoggingConfigSection);

if (builder.Environment.IsDevelopment())
{
    // only add NLog for DEV, otherwise it will try log to local/docker udp port 9999 in PROD
    builder.Logging.AddNLogWeb(new NLog.Config.LoggingConfiguration().AddLocalLog4ViewerNLogTarget());

    var localDevelopmentOptionsConfigSection = builder.Configuration.GetSection("LocalDevelopment");
    builder.Services.AddLocalDevelopmentServices(localDevelopmentOptionsConfigSection);
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    var autoMapper = app.Services.GetRequiredService<IMapper>();
    autoMapper.ConfigurationProvider.AssertConfigurationIsValid(); // this is expensive on startup, so only do it in Dev environment, unit tests will fail before reaching PROD
}

// app.UseHttpsRedirection(); // this is not necessary in Google Cloud Run, they enforce HTTPs for external connections but the app in the container runs on HTTP

app.UseAuthorization();

app.MapControllers();

var port = Environment.GetEnvironmentVariable("PORT");
if (port != null)
{
    app.Urls.Add($"http://0.0.0.0:{port}");
}

try
{
    app.Run();
}
finally
{
    // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
    NLog.LogManager.Shutdown();
}