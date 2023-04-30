using Firepuma.CommandsAndQueries.Abstractions.Commands;
using Firepuma.Scheduling.Domain.IntegrationEvents;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Abstractions;
using Firepuma.Scheduling.Domain.Plumbing.IntegrationEvents.Services;
using MediatR;

namespace Firepuma.Scheduling.Domain.ScheduledTasks.Commands;

public class HandleScheduledJob : BaseCommand
{
    public required string ScheduledJobName { get; init; }

    // ReSharper disable once UnusedType.Global
    public class CommandHandler : IRequestHandler<HandleScheduledJob>
    {
        private readonly ICommandEventPublisher _commandEventPublisher;

        public CommandHandler(
            ICommandEventPublisher commandEventPublisher)
        {
            _commandEventPublisher = commandEventPublisher;
        }

        public async Task Handle(HandleScheduledJob request, CancellationToken cancellationToken)
        {
            object integrationEvent = request.ScheduledJobName switch
            {
                "notify-due-tasks" => new ScheduledNotifyDueTasks { CommandId = request.CommandId },

                _ => throw new Exception($"ScheduledJobName '{request.ScheduledJobName}' is not supported"),
            };

            await _commandEventPublisher.PublishAsync(
                request,
                integrationEvent,
                new SendEventSelfTarget(),
                cancellationToken);
        }
    }
}