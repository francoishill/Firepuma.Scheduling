using Firepuma.CommandsAndQueries.Abstractions.Commands;
using Firepuma.CommandsAndQueries.Abstractions.Queries;
using Firepuma.Scheduling.Domain.Plumbing.Reflection;

namespace Firepuma.Scheduling.Infrastructure.Plumbing.CommandHandling.Assertions;

public static class CommandHandlingAssertionHelpers
{
    public static IEnumerable<Type> GetAllCommandTypes() => ReflectionHelpers.GetAllLoadableTypes().Where(IsCommandRequest);
    public static IEnumerable<Type> GetAllQueryTypes() => ReflectionHelpers.GetAllLoadableTypes().Where(IsQueryRequest);
    // public static IEnumerable<Type> GetAllAuthorizationRequirementTypes() => ReflectionHelpers.GetAllLoadableTypes().Where(IsIAuthorizationRequirementRequest);

    private static bool IsCommandRequest(Type type) => !type.IsAbstract && typeof(ICommandRequest).IsAssignableFrom(type);
    private static bool IsQueryRequest(Type type) => !type.IsAbstract && typeof(IQueryRequest).IsAssignableFrom(type);
    // private static bool IsIAuthorizationRequirementRequest(Type type) => !type.IsAbstract && typeof(IAuthorizationRequirement).IsAssignableFrom(type);
}