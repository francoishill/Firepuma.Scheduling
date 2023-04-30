using Firepuma.CommandsAndQueries.Abstractions.Commands;
using Firepuma.CommandsAndQueries.Abstractions.PipelineBehaviors.Helpers;
using Firepuma.Scheduling.Domain.Plumbing.CommandHandling.Entities;
using Firepuma.Scheduling.Domain.Plumbing.CommandHandling.Extensions;
using Firepuma.Scheduling.Domain.Plumbing.CommandHandling.Repositories;
using Firepuma.Scheduling.Domain.Plumbing.CommandHandling.Services;
using MediatR;

namespace Firepuma.Scheduling.Domain.Plumbing.CommandHandling.PipelineBehaviors;

/// <summary>
/// This pipeline is responsible for creating an instance of <see cref="CommandExecutionMongoDbEvent"/>, storing it
/// in the database, executing the <c>next()</c> delegate in a try-catch and finally storing the result. The result
/// information contains success boolean Result/Error, durations, etc.
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResponse"></typeparam>
public class CommandExecutionRecordingPipeline<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse?>
    where TRequest : ICommandRequest
{
    private readonly ICommandExecutionRepository _commandExecutionRepository;
    private readonly ICommandContext _commandContext;

    public CommandExecutionRecordingPipeline(
        ICommandExecutionRepository commandExecutionRepository,
        ICommandContext commandContext)
    {
        _commandExecutionRepository = commandExecutionRepository;
        _commandContext = commandContext;
    }

    public async Task<TResponse?> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse?> next,
        CancellationToken cancellationToken)
    {
        var executionEvent = new CommandExecutionMongoDbEvent
        {
            Id = CommandExecutionMongoDbEvent.GenerateId(),
        };

        CommandExecutionHelpers.PopulateExecutionEventBeforeStart(request, executionEvent);

        executionEvent = await _commandExecutionRepository.AddItemAsync(
            executionEvent,
            cancellationToken);

        _commandContext.SetCommandExecutionEvent(request, executionEvent);

        try
        {
            return await CommandExecutionHelpers.ExecuteCommandAsync(
                next,
                request,
                executionEvent);
        }
        finally
        {
            // ReSharper disable once RedundantAssignment
            executionEvent = await _commandExecutionRepository.ReplaceItemAsync(
                executionEvent,
                cancellationToken);
        }
    }
}