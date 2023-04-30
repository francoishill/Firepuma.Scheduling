using Firepuma.EventMediation.IntegrationEvents.ValueObjects;
using Firepuma.Scheduling.Domain.IntegrationEvents;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Abstractions;
using Firepuma.Scheduling.Domain.Repositories;
using MediatR;

namespace Firepuma.Scheduling.Worker.Plumbing.LocalDevelopment.Services;

internal class LocalDevelopmentNotifyDueTasksService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMediator _mediator;

    public LocalDevelopmentNotifyDueTasksService(
        IServiceProvider serviceProvider,
        IMediator mediator)
    {
        _serviceProvider = serviceProvider;
        _mediator = mediator;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var scheduledNotifyDueTasksCommandsFactory = new ScheduledNotifyDueTasks.CommandsFactory(
                _serviceProvider.GetRequiredService<ILogger<ScheduledNotifyDueTasks.CommandsFactory>>(),
                _serviceProvider.GetRequiredService<IScheduledTaskRepository>());

            var commands = await scheduledNotifyDueTasksCommandsFactory.Handle(
                new CreateCommandsFromIntegrationEventRequest<ScheduledNotifyDueTasks>(new IntegrationEventEnvelope
                    {
                        EventId = Guid.NewGuid().ToString(),
                        EventType = "[PLACEHOLDER_FROM_LocalDevelopmentNotifyDueTasks]",
                        EventPayload = "",
                    },
                    new ScheduledNotifyDueTasks
                    {
                        CommandId = Guid.NewGuid().ToString(),
                    }),
                stoppingToken);

            var commandTasks = commands
                .Select(command => _mediator.Send(command, stoppingToken));

            await Task.WhenAll(commandTasks);

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}