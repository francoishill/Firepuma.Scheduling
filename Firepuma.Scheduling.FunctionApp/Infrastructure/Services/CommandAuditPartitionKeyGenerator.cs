using Firepuma.CommandsAndQueries.Abstractions.Entities;
using Firepuma.CommandsAndQueries.CosmosDb.Services;

// ReSharper disable ClassNeverInstantiated.Global

namespace Firepuma.Scheduling.FunctionApp.Infrastructure.Services;

public class CommandAuditPartitionKeyGenerator : ICommandAuditPartitionKeyGenerator
{
    public string GeneratePartitionKey(CommandExecutionEvent entity)
    {
        return entity.CreatedOn.ToString("yyyy-MM");
    }
}