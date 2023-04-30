using Firepuma.CommandsAndQueries.Abstractions.Entities;
using Firepuma.Scheduling.Domain.Admin.Queries.Filters;

namespace Firepuma.Scheduling.Domain.Admin.Services;

public interface ICommandExecutionQueryService
{
    Task<long> GetItemsCountAsync(
        CommandExecutionEventFilter? filter,
        CancellationToken cancellationToken);

    Task<IEnumerable<ICommandExecutionEvent>> GetItemsAsync(
        CommandExecutionEventFilter? filter,
        int? offset,
        int? limit,
        CancellationToken cancellationToken);
}