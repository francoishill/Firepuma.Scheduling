using System.Diagnostics.CodeAnalysis;
using Firepuma.CommandsAndQueries.Abstractions.Commands;
using Firepuma.Scheduling.Domain.Plumbing.CommandHandling.Entities;
using Firepuma.Scheduling.Domain.Plumbing.CommandHandling.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Firepuma.Scheduling.Domain.Plumbing.CommandHandling.Extensions;

public static class CommandContextExtensions
{
    /// <summary>
    /// Link the execution event in-memory to the <c>commandRequest</c>.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="commandRequest"></param>
    /// <param name="executionEvent"></param>
    public static void SetCommandExecutionEvent(
        this ICommandContext context,
        ICommandRequest commandRequest,
        CommandExecutionMongoDbEvent executionEvent)
    {
        context.Set(
            commandRequest,
            ContextPropertyKeys.EXECUTION_EVENT,
            executionEvent,
            new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(30)));
    }

    /// <summary>
    /// Try get the execution event that was added by a previous call to <see cref="SetCommandExecutionEvent" />.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="commandRequest"></param>
    /// <param name="commandExecutionEvent"></param>
    /// <returns></returns>
    public static bool TryGetCommandExecutionEvent(
        this ICommandContext context,
        ICommandRequest commandRequest,
        [NotNullWhen(true)] out CommandExecutionMongoDbEvent? commandExecutionEvent)
    {
        return context.TryGetValue(
            commandRequest,
            ContextPropertyKeys.EXECUTION_EVENT,
            out commandExecutionEvent);
    }

    private static class ContextPropertyKeys
    {
        public const string EXECUTION_EVENT = "ExecutionEvent";
    }
}