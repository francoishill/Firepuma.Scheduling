using System.Diagnostics.CodeAnalysis;
using Firepuma.CommandsAndQueries.Abstractions.Commands;
using Microsoft.Extensions.Caching.Memory;

namespace Firepuma.Scheduling.Domain.Plumbing.CommandHandling.Services;

/// <summary>
/// A service for linking values in-memory to a command request.
/// </summary>
public interface ICommandContext
{
    void Set<TItem>(
        ICommandRequest commandRequest,
        object propertyKey,
        TItem propertyValue,
        MemoryCacheEntryOptions cacheEntryOptions);

    bool TryGetValue<TValue>(
        ICommandRequest commandRequest,
        object propertyKey,
        [NotNullWhen(true)] out TValue? value);
}