using Firepuma.Scheduling.Domain.Plumbing.Reflection;
using Firepuma.Scheduling.Infrastructure.Plumbing.CommandHandling.Assertions;
using Firepuma.Scheduling.Tests.IntegrationEvents;
using Xunit.Abstractions;

namespace Firepuma.Scheduling.Tests.CommandHandling;

public class CommandHandlerTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public CommandHandlerTests(
        ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Theory]
    [MemberData(nameof(CommandTypes))]
    public void Commands_have_single_handler(Type commandType)
    {
        _testOutputHelper.WriteLine($"Testing {commandType.FullName ?? commandType.Name}");
        var registeredHandlers = ReflectionHelpers.GetRegisteredHandlersForRequestType(commandType).ToList();

        if (registeredHandlers.Count != 1)
        {
            throw new Exception(
                $"{commandType.FullName} has {registeredHandlers.Count} handlers but it " +
                $"should have 1, handler types: {string.Join(", ", registeredHandlers.Select(h => h.FullName))}");
        }
    }

    [Theory]
    [MemberData(nameof(QueryTypes))]
    public void Queries_have_single_handler(Type queryType)
    {
        _testOutputHelper.WriteLine($"Testing {queryType.FullName ?? queryType.Name}");
        var registeredHandlers = ReflectionHelpers.GetRegisteredHandlersForRequestType(queryType).ToList();

        if (registeredHandlers.Count != 1)
        {
            throw new Exception(
                $"{queryType.FullName} has {registeredHandlers.Count} handlers but it " +
                $"should have 1, handler types: {string.Join(", ", registeredHandlers.Select(h => h.FullName))}");
        }
    }

    // [Theory]
    // [MemberData(nameof(AuthorizationRequirementTypes))]
    // public void AuthorizationRequirements_have_single_handler(Type requirementType)
    // {
    //     _testOutputHelper.WriteLine($"Testing {requirementType.FullName ?? requirementType.Name}");
    //     var registeredHandlers = ReflectionHelpers.GetRegisteredHandlersForRequestType(requirementType).ToList();
    //
    //     if (registeredHandlers.Count != 1)
    //     {
    //         throw new Exception(
    //             $"{requirementType.FullName} has {registeredHandlers.Count} handlers but it " +
    //             $"should have 1, handler types: {string.Join(", ", registeredHandlers.Select(h => h.FullName))}");
    //     }
    // }

    [Fact] public void CommandTypes_enumerable_is_not_empty() => Assert.NotEmpty(CommandTypes);
    [Fact] public void QueryTypes_enumerable_is_not_empty() => Assert.NotEmpty(QueryTypes);
    // [Fact] public void AuthorizationRequirementTypes_enumerable_is_not_empty() => Assert.NotEmpty(AuthorizationRequirementTypes);

    public static IEnumerable<object[]> CommandTypes =>
        CommandHandlingAssertionHelpers.GetAllCommandTypes()
            .Where(type => type != typeof(CommandEventPublisherTests.MockEmptyCommand))
            .Select(type => new object[] { type });

    public static IEnumerable<object[]> QueryTypes =>
        CommandHandlingAssertionHelpers.GetAllQueryTypes().Select(type => new object[] { type });
}