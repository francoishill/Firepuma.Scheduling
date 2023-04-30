using Firepuma.CommandsAndQueries.Abstractions.Queries;
using Firepuma.Scheduling.Domain.Admin.Queries.Filters;
using Firepuma.Scheduling.Domain.Admin.Services;
using MediatR;

namespace Firepuma.Scheduling.Domain.Admin.Queries;

public class CountCommandExecutionEvents : BaseQuery<long>
{
    public required CommandExecutionEventFilter? Filter { get; init; }

    // ReSharper disable once UnusedType.Global
    public class QueryHandler : IRequestHandler<CountCommandExecutionEvents, long>
    {
        private readonly ICommandExecutionQueryService _commandExecutionQueryService;

        public QueryHandler(
            ICommandExecutionQueryService commandExecutionQueryService)
        {
            _commandExecutionQueryService = commandExecutionQueryService;
        }

        public async Task<long> Handle(CountCommandExecutionEvents request, CancellationToken cancellationToken)
        {
            var count = await _commandExecutionQueryService.GetItemsCountAsync(request.Filter, cancellationToken);
            return count;
        }
    }
}