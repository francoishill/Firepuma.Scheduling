﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Firepuma.Scheduling.FunctionApp.Abstractions.Entities;
using Firepuma.Scheduling.FunctionApp.Abstractions.Repositories;
using Firepuma.Scheduling.FunctionApp.Abstractions.Specifications;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;

// ReSharper disable RedundantTypeArgumentsOfMethod

namespace Firepuma.Scheduling.FunctionApp.Infrastructure.CosmosDb;

public abstract class CosmosDbRepository<T> : IRepository<T> where T : BaseEntity, new()
{
    protected readonly ILogger Logger;
    protected readonly Container Container;

    protected CosmosDbRepository(
        ILogger logger,
        Container container)
    {
        Logger = logger;
        Container = container;
    }

    protected abstract string GenerateId(T entity);
    protected abstract PartitionKey ResolvePartitionKey(string entityId);

    public async Task<IEnumerable<T>> GetItemsAsync(
        ISpecification<T> specification,
        CancellationToken cancellationToken)
    {
        var queryable = ApplySpecification(specification);
        var iterator = queryable.ToFeedIterator<T>();

        var totalRequestCharge = 0D;

        var results = new List<T>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);

            results.AddRange(response.ToList());

            Logger.LogDebug(
                "Fetching {Count} items from container {Container} consumed {Charge} RUs",
                response.Count, Container.Id, response.RequestCharge);

            totalRequestCharge += response.RequestCharge;
        }

        Logger.LogInformation(
            "A total of {Count} items were fetched from container {Container} and consumed total {Charge} RUs",
            results.Count, Container.Id, totalRequestCharge);

        return results;
    }

    public async Task<IEnumerable<T>> GetItemsAsync(
        string queryString,
        CancellationToken cancellationToken)
    {
        var resultSetIterator = Container.GetItemQueryIterator<T>(new QueryDefinition(queryString));

        var totalRequestCharge = 0D;

        var results = new List<T>();
        while (resultSetIterator.HasMoreResults)
        {
            var response = await resultSetIterator.ReadNextAsync(cancellationToken);

            results.AddRange(response.ToList());

            Logger.LogDebug(
                "Fetching {Count} items (with query {Query}) from container {Container} consumed {Charge} RUs",
                response.Count, queryString, Container.Id, response.RequestCharge);

            totalRequestCharge += response.RequestCharge;
        }

        Logger.LogInformation(
            "A total of {Count} items were fetched from container {Container} and consumed total {Charge} RUs",
            results.Count, Container.Id, totalRequestCharge);

        return results;
    }

    public async Task<int> GetItemsCountAsync(
        ISpecification<T> specification,
        CancellationToken cancellationToken)
    {
        var queryable = ApplySpecification(specification);

        var response = await queryable.CountAsync(cancellationToken: cancellationToken);

        Logger.LogInformation(
            "Counting items from container {Container} consumed {Charge} RUs",
            Container.Id, response.RequestCharge);

        return response.Resource;
    }

    public async Task<T> GetItemOrDefaultAsync(
        string id,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await Container.ReadItemAsync<T>(id, ResolvePartitionKey(id), cancellationToken: cancellationToken);

            Logger.LogInformation(
                "Fetching item id {Id} from container {Container} consumed {Charge} RUs",
                id, Container.Id, response.RequestCharge);

            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task AddItemAsync(
        T item,
        CancellationToken cancellationToken)
    {
        item.Id = GenerateId(item);

        var response = await Container.CreateItemAsync<T>(item, ResolvePartitionKey(item.Id), cancellationToken: cancellationToken);

        item.ETag = response.ETag;

        Logger.LogInformation(
            "Adding item id {Id} to container {Container} consumed {Charge} RUs",
            item.Id, Container.Id, response.RequestCharge);
    }

    public async Task UpsertItemAsync(
        T item,
        bool ignoreETag,
        CancellationToken cancellationToken)
    {
        var options = new ItemRequestOptions();

        if (!ignoreETag)
        {
            options.IfMatchEtag = item.ETag;
        }

        item.Id ??= GenerateId(item);

        var response = await Container.UpsertItemAsync<T>(item, ResolvePartitionKey(item.Id), options, cancellationToken);

        item.ETag = response.ETag;

        Logger.LogInformation(
            "Upserted item id {Id} in container {Container} consumed {Charge} RUs",
            item.Id, Container.Id, response.RequestCharge);
    }

    public async Task DeleteItemAsync(
        T item,
        bool ignoreETag,
        CancellationToken cancellationToken)
    {
        var options = new ItemRequestOptions();

        if (!ignoreETag)
        {
            options.IfMatchEtag = item.ETag;
        }

        var response = await Container.DeleteItemAsync<T>(item.Id, ResolvePartitionKey(item.Id), options, cancellationToken);

        Logger.LogInformation(
            "Deleting item id {Id} from container {Container} consumed {Charge} RUs",
            item.Id, Container.Id, response.RequestCharge);
    }

    private IQueryable<T> ApplySpecification(ISpecification<T> specification)
    {
        var evaluator = new CosmosDbSpecificationEvaluator<T>();
        return evaluator.GetQuery(Container.GetItemLinqQueryable<T>(), specification);
    }
}