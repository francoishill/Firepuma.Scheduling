using Firepuma.Scheduling.Domain.Commands;
using Firepuma.Scheduling.Domain.QuerySpecifications;
using Firepuma.Scheduling.Domain.Repositories;
using MediatR;

namespace Firepuma.Scheduling.Worker.Plumbing.LocalDevelopment.Services;

internal class LocalDevelopmentNotifyDueTasksService : BackgroundService
{
    private readonly ILogger<LocalDevelopmentNotifyDueTasksService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMediator _mediator;

    public LocalDevelopmentNotifyDueTasksService(
        ILogger<LocalDevelopmentNotifyDueTasksService> logger,
        IServiceProvider serviceProvider,
        IMediator mediator)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _mediator = mediator;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(5);

        while (!stoppingToken.IsCancellationRequested)
        {
            var scheduledTaskRepository = _serviceProvider.GetRequiredService<IScheduledTaskRepository>();

            var nowWithAddedBufferForProcessingTime = DateTime.UtcNow.Add(interval);
            var querySpecification = new ScheduledTaskQuerySpecifications.DueNow(nowWithAddedBufferForProcessingTime);

            var scheduledTasks = (await scheduledTaskRepository.GetItemsAsync(querySpecification, stoppingToken)).ToList();

            if (scheduledTasks.Any())
            {
                _logger.LogInformation("Notifying applications of {Count} due tasks", scheduledTasks.Count);

                var commands = scheduledTasks
                    .Select(task => new NotifyDueTask
                    {
                        ScheduledTask = task,
                    });

                var commandTasks = commands
                    .Select(command => _mediator.Send(command, stoppingToken));

                await Task.WhenAll(commandTasks);
            }

            await Task.Delay(interval, stoppingToken);
        }
    }
}