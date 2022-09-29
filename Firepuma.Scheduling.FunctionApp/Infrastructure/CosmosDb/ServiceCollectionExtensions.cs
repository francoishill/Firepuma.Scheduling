using System;
using Firepuma.Scheduling.FunctionApp.Abstractions.Entities;
using Firepuma.Scheduling.FunctionApp.Abstractions.Repositories;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Firepuma.Scheduling.FunctionApp.Infrastructure.CosmosDb;

public static class ServiceCollectionExtensions
{
    public static void AddCosmosDb(
        this IServiceCollection services,
        string connectionString,
        string databaseId)
    {
        var client = new CosmosClient(connectionString);
        var database = client.GetDatabase(databaseId);
        services.AddSingleton(_ => database);
    }

    public static void AddCosmosDbRepository<TEntity, TInterface, TClass>(
        this IServiceCollection services,
        string containerName,
        Func<ILogger<TClass>, Container, TClass> classFactory)
        where TEntity : BaseEntity, new()
        where TInterface : class, IRepository<TEntity>
        where TClass : class, TInterface
    {
        services.AddSingleton<TInterface, TClass>(s =>
        {
            var logger = s.GetRequiredService<ILogger<TClass>>();

            var database = s.GetRequiredService<Database>();
            var container = database.GetContainer(containerName);

            return classFactory(logger, container);
        });
    }
}