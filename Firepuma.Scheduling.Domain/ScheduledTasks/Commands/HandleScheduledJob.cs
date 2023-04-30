using Firepuma.CommandsAndQueries.Abstractions.Commands;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Firepuma.Scheduling.Domain.ScheduledTasks.Commands;

public class HandleScheduledJob : BaseCommand
{
    public required string ScheduledJobName { get; init; }

    // ReSharper disable once UnusedType.Global
    public class CommandHandler : IRequestHandler<HandleScheduledJob>
    {
        private readonly ILogger<CommandHandler> _logger;

        public CommandHandler(
            ILogger<CommandHandler> logger)
        {
            _logger = logger;
        }

        public async Task Handle(HandleScheduledJob request, CancellationToken cancellationToken)
        {
            _logger.LogError(
                "HandleScheduledJob is not implemented and cannot execute {ScheduledJobName}, see commented code below for example implementation",
                request.ScheduledJobName);

            await Task.CompletedTask;
        }

        // private readonly ICommandEventPublisher _commandEventPublisher;
        //
        // public CommandHandler(
        //     ICommandEventPublisher commandEventPublisher)
        // {
        //     _commandEventPublisher = commandEventPublisher;
        // }
        //
        // public async Task Handle(HandleScheduledJob request, CancellationToken cancellationToken)
        // {
        //     object integrationEvent = request.ScheduledJobName switch
        //     {
        //         "notify-own-birthday" => new ScheduledNotifyOwnBirthdays { CommandId = request.CommandId },
        //
        //         _ => throw new Exception($"ScheduledJobName '{request.ScheduledJobName}' is not supported"),
        //     };
        //
        //     await _commandEventPublisher.PublishAsync(
        //         request,
        //         integrationEvent,
        //         new SendEventSelfTarget(),
        //         cancellationToken);
        // }
    }
}