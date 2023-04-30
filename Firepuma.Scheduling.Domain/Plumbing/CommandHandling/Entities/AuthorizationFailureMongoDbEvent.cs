using System.Diagnostics;
using Firepuma.CommandsAndQueries.Abstractions.Entities;
using Firepuma.DatabaseRepositories.MongoDb.Abstractions.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

#pragma warning disable CS8618
// ReSharper disable EmptyConstructor

namespace Firepuma.Scheduling.Domain.Plumbing.CommandHandling.Entities;

/// <summary>
/// When authorization fails for a domain request (Command/Query), the event information is
/// stored in MongoDb with the list of FailedRequirements.
/// <seealso cref="PipelineBehaviors.PrerequisitesPipelineBehavior{TRequest,TResponse}"/> 
/// </summary>
[DebuggerDisplay("{ToString()}")]
public class AuthorizationFailureMongoDbEvent : BaseMongoDbEntity, IAuthorizationFailureEvent
{
    public DateTime CreatedOn { get; set; }

    public string ActionTypeName { get; set; }
    public string ActionTypeNamespace { get; set; }
    public object? ActionPayload { get; set; }
    public FailedAuthorizationRequirement[] FailedRequirements { get; set; }

    public AuthorizationFailureMongoDbEvent()
    {
        // Typically used by Database repository deserialization (including the Add methods, like repository.AddItemAsync)
    }

    public static string GenerateId() => ObjectId.GenerateNewId().ToString();

    public override string ToString()
    {
        return $"{Id}/{ActionTypeName}/{ActionTypeNamespace}";
    }

    public static IEnumerable<CreateIndexModel<AuthorizationFailureMongoDbEvent>> GetSchemaIndexes()
    {
        return Array.Empty<CreateIndexModel<AuthorizationFailureMongoDbEvent>>();
    }
}