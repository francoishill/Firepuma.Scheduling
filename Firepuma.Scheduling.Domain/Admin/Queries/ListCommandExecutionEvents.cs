using Firepuma.CommandsAndQueries.Abstractions.Entities;
using Firepuma.CommandsAndQueries.Abstractions.Queries;
using Firepuma.Scheduling.Domain.Admin.Queries.Filters;
using Firepuma.Scheduling.Domain.Admin.Services;
using Firepuma.Scheduling.Domain.Plumbing.Pagination;
using MediatR;

namespace Firepuma.Scheduling.Domain.Admin.Queries;

public class ListCommandExecutionEvents : BaseQuery<PaginatedItems<ICommandExecutionEvent>>
{
    public required int PageIndex { get; init; }
    public required int PageSize { get; init; }
    public required CommandExecutionEventFilter? Filter { get; init; }

    // ReSharper disable once UnusedType.Global
    public class QueryHandler : IRequestHandler<ListCommandExecutionEvents, PaginatedItems<ICommandExecutionEvent>>
    {
        private readonly ICommandExecutionQueryService _commandExecutionQueryService;

        public QueryHandler(
            ICommandExecutionQueryService commandExecutionQueryService)
        {
            _commandExecutionQueryService = commandExecutionQueryService;
        }

        public async Task<PaginatedItems<ICommandExecutionEvent>> Handle(ListCommandExecutionEvents request, CancellationToken cancellationToken)
        {
            var totalCountTask = _commandExecutionQueryService.GetItemsCountAsync(request.Filter, cancellationToken);

            var listTask = _commandExecutionQueryService.GetItemsAsync(
                request.Filter,
                offset: request.PageIndex * request.PageSize,
                limit: request.PageSize,
                cancellationToken);

            await Task.WhenAll(totalCountTask, listTask);

            return new PaginatedItems<ICommandExecutionEvent>(listTask.Result, request.PageIndex, request.PageSize, totalCountTask.Result);
        }
    }
}