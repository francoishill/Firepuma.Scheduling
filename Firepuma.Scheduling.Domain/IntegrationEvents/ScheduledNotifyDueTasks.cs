using Firepuma.CommandsAndQueries.Abstractions.Commands;
using Firepuma.Scheduling.Domain.Commands;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Abstractions;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Attributes;
using Firepuma.Scheduling.Domain.QuerySpecifications;
using Firepuma.Scheduling.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Firepuma.Scheduling.Domain.IntegrationEvents;

[OutgoingIntegrationEventType("Firepuma/Scheduling/Request/ScheduledNotifyDueTasks")]
[IncomingIntegrationEventType("Firepuma/Scheduling/Request/ScheduledNotifyDueTasks")]
public class ScheduledNotifyDueTasks : BaseOutgoingIntegrationEvent
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class CommandsFactory : ICommandsFactory<ScheduledNotifyDueTasks>
    {
        private readonly ILogger<CommandsFactory> _logger;
        private readonly IScheduledTaskRepository _scheduledTaskRepository;

        public CommandsFactory(
            ILogger<CommandsFactory> logger,
            IScheduledTaskRepository scheduledTaskRepository)
        {
            _logger = logger;
            _scheduledTaskRepository = scheduledTaskRepository;
        }

        public async Task<IEnumerable<ICommandRequest>> Handle(
            CreateCommandsFromIntegrationEventRequest<ScheduledNotifyDueTasks> request,
            CancellationToken cancellationToken)
        {
            var checkAheadDuration = TimeSpan.FromSeconds(55);
            var nowWithAddedBufferForProcessingTime = DateTime.UtcNow.Add(checkAheadDuration);
            var querySpecification = new ScheduledTaskQuerySpecifications.DueNow(nowWithAddedBufferForProcessingTime);

            var scheduledTasks = (await _scheduledTaskRepository.GetItemsAsync(querySpecification, cancellationToken)).ToList();

            if (scheduledTasks.Any())
            {
                _logger.LogInformation("Notifying applications of {Count} due tasks", scheduledTasks.Count);
            }

            return scheduledTasks
                .Select(task => new NotifyDueTask
                {
                    ScheduledTask = task,
                });
        }
    }
}