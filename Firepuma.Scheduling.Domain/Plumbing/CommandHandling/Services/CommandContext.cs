using System.Diagnostics.CodeAnalysis;
using Firepuma.CommandsAndQueries.Abstractions.Commands;
using Microsoft.Extensions.Caching.Memory;

namespace Firepuma.Scheduling.Domain.Plumbing.CommandHandling.Services;

public class CommandContext : ICommandContext
{
    private readonly IMemoryCache _memoryCache;

    public CommandContext(
        IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public void Set<TItem>(
        ICommandRequest commandRequest,
        object propertyKey,
        TItem propertyValue,
        MemoryCacheEntryOptions cacheEntryOptions)
    {
        var cacheKey = GetCacheKeyFromCommandRequest(commandRequest);

        var cachedProperties = _memoryCache.GetOrCreate(
            cacheKey,
            cacheEntry =>
            {
                cacheEntry.SetOptions(cacheEntryOptions);

                return new Dictionary<object, object?>();
            });

        // this is not a thread-safe dictionary but should be okay for now since its instance is for a single command
        cachedProperties![propertyKey] = propertyValue;
    }

    public bool TryGetValue<TValue>(
        ICommandRequest commandRequest,
        object propertyKey,
        [NotNullWhen(true)] out TValue? value)
    {
        var cacheKey = GetCacheKeyFromCommandRequest(commandRequest);

        var cachedProperties = _memoryCache.Get<Dictionary<object, object?>>(cacheKey);

        if (cachedProperties == null)
        {
            value = default;
            return false;
        }

        if (!cachedProperties.TryGetValue(propertyKey, out var valueObject))
        {
            value = default;
            return false;
        }

        if (valueObject is not TValue typeSafeValue)
        {
            value = default;
            return false;
        }

        value = typeSafeValue;
        return true;
    }

    private static object GetCacheKeyFromCommandRequest(ICommandRequest commandRequest) => commandRequest;
}