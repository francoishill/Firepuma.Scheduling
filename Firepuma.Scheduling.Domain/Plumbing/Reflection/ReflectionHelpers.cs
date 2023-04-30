using System.Reflection;
using MediatR;

namespace Firepuma.Scheduling.Domain.Plumbing.Reflection;

public static class ReflectionHelpers
{
    public static IEnumerable<Type> GetAllLoadableTypes()
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(assembly => assembly.GetLoadableTypes());
    }

    public static IEnumerable<Type> GetRegisteredHandlersForRequestType(Type requestTyp)
    {
        return GetAllLoadableTypes()
            .Where(IsIRequestHandler)
            .Where(handlerType => IsHandlerForRequest(handlerType, requestTyp));
    }

    private static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            return e.Types.Where(t => t != null)!;
        }
    }

    private static bool IsHandlerForRequest(Type handlerType, Type requestType)
    {
        return handlerType.GetInterfaces().Any(i => i.GenericTypeArguments.Any(ta => ta == requestType));
    }

    private static bool IsIRequestHandler(Type type)
    {
        return type.GetInterfaces().Any(interfaceType =>
            interfaceType.IsGenericType
            && (
                interfaceType.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)
                || interfaceType.GetGenericTypeDefinition() == typeof(IRequestHandler<>)));
    }
}