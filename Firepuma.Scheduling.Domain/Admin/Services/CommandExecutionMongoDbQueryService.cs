using System.Linq.Expressions;
using Firepuma.CommandsAndQueries.Abstractions.Entities;
using Firepuma.DatabaseRepositories.Abstractions.QuerySpecifications;
using Firepuma.Scheduling.Domain.Admin.Queries.Filters;
using Firepuma.Scheduling.Domain.Plumbing.CommandHandling.Entities;
using Firepuma.Scheduling.Domain.Plumbing.CommandHandling.Repositories;

namespace Firepuma.Scheduling.Domain.Admin.Services;

public class CommandExecutionMongoDbQueryService : ICommandExecutionQueryService
{
    private readonly ICommandExecutionRepository _commandExecutionRepository;

    public CommandExecutionMongoDbQueryService(
        ICommandExecutionRepository commandExecutionRepository)
    {
        _commandExecutionRepository = commandExecutionRepository;
    }

    public async Task<long> GetItemsCountAsync(CommandExecutionEventFilter? filter, CancellationToken cancellationToken)
    {
        var querySpecification = new QuerySpecification<CommandExecutionMongoDbEvent>();

        if (filter != null)
        {
            querySpecification.WhereExpressions.AddRange(GetWhereClauses(filter));
        }

        var count = await _commandExecutionRepository.GetItemsCountAsync(querySpecification, cancellationToken);

        return count;
    }

    public async Task<IEnumerable<ICommandExecutionEvent>> GetItemsAsync(
        CommandExecutionEventFilter? filter,
        int? offset,
        int? limit,
        CancellationToken cancellationToken)
    {
        var querySpecification = new QuerySpecification<CommandExecutionMongoDbEvent>();

        if (filter != null)
        {
            querySpecification.WhereExpressions.AddRange(GetWhereClauses(filter));
        }

        querySpecification.OrderExpressions.Add((r => r.CreatedOn, OrderTypeEnum.Descending));

        querySpecification.Skip = offset;
        querySpecification.Take = limit;

        var executionEvents = await _commandExecutionRepository.GetItemsAsync(querySpecification, cancellationToken);

        return executionEvents;
    }

    private IEnumerable<Expression<Func<CommandExecutionMongoDbEvent, bool>>> GetWhereClauses(CommandExecutionEventFilter filter)
    {
        var whereClauses = new List<Expression<Func<CommandExecutionMongoDbEvent, bool>>>();

        if (!string.IsNullOrWhiteSpace(filter.TextSearch))
        {
            var textSearchLowerCase = filter.TextSearch.ToLower();

            whereClauses.Add(x =>
                x.TypeName.ToLower().Contains(textSearchLowerCase)
                || x.TypeNamespace.ToLower().Contains(textSearchLowerCase)
                //TODO: Add this if the project has Users and an ActorContext
                // || (x.ActorContext != null && x.ActorContext.ActorName.ToLower().Contains(textSearchLowerCase))
                || x.ErrorMessage!.ToLower().Contains(textSearchLowerCase)
                || x.ErrorStackTrack!.ToLower().Contains(textSearchLowerCase)
                || x.Payload.ToLower().Contains(textSearchLowerCase)
                || x.Result!.ToLower().Contains(textSearchLowerCase)
            );
        }

        if (filter.FilterSuccessful.HasValue)
        {
            whereClauses.Add(filter.FilterSuccessful.Value switch
            {
                CommandExecutionEventFilter.SuccessfulFilter.True => x => x.Successful == true,
                CommandExecutionEventFilter.SuccessfulFilter.False => x => x.Successful == false,
                CommandExecutionEventFilter.SuccessfulFilter.Null => x => x.Successful == null,
                _ => throw new ArgumentOutOfRangeException(),
            });
        }

        if (filter.FilterTypeName != null)
        {
            whereClauses.Add(x => x.TypeName == filter.FilterTypeName);
        }

        if (filter.MinimumCreatedOn != null)
        {
            whereClauses.Add(x => x.CreatedOn >= filter.MinimumCreatedOn);
        }

        if (filter.MaximumCreatedOn != null)
        {
            whereClauses.Add(x => x.CreatedOn <= filter.MaximumCreatedOn);
        }

        //TODO: Add this if the project has Users and an ActorContext
        // if (filter.ActorId != null)
        // {
        //     whereClauses.Add(x => x.ActorContext != null && x.ActorContext.ActorId == filter.ActorId);
        // }

        if (filter.MinimumExecutionSeconds != null)
        {
            whereClauses.Add(x => x.ExecutionTimeInSeconds >= filter.MinimumExecutionSeconds.Value);
        }

        if (filter.MaximumExecutionSeconds != null)
        {
            whereClauses.Add(x => x.ExecutionTimeInSeconds <= filter.MaximumExecutionSeconds.Value);
        }

        if (filter.MinimumTotalDuration != null)
        {
            whereClauses.Add(x => x.TotalTimeInSeconds >= filter.MinimumTotalDuration.Value);
        }

        if (filter.MaximumTotalDuration != null)
        {
            whereClauses.Add(x => x.TotalTimeInSeconds <= filter.MaximumTotalDuration.Value);
        }

        return whereClauses;
    }
}