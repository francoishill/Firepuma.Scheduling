# Firepuma.Scheduling

## TODO (after creating project from template)

* [ ] Decide if you need the WebApi project (typically for user/browser facing apps), if not and it is only a microservice, then:
    * [ ] move the "LocalDevelopment" directory out of WebApi into Worker before deleting WebApi
    * [ ] delete its project (Firepuma.Scheduling.WebApi)
    * [ ] delete its github workflow (.github/workflows/deploy-gcloud-run-webapi.yml)
    * [ ] delete its references in .github/workflows/unit-tests-only.yml
* [ ] The template will generate random HTTP and HTTPS ports and replace them in the following file (open the file to ensure the ports make sense and don't collide with another service):
    * [ ] `Firepuma.Scheduling.WebApi/Properties/launchSettings.json`
    * [ ] `Firepuma.Scheduling.Worker/Properties/launchSettings.json`
* [ ] Reuse or delete the following files / references:
    * [ ] Fix the missing files in `SolutionFiles` solution folder
    * [ ] `Domain/Pets`
    * [ ] `Infrastructure/Pets`
    * [ ] `WebApi/Pets`
    * [ ] References to `PetsCollectionName` (in `appsettings.json`) and in `MongoDbOptions` class
    * [ ] Any other references to `Pets`
    * [ ] Look at `Domain/Plumbing/IntegrationEvents/Services/IntegrationEventsMappingCache.cs`
    * [ ] Ensure the `SERVICE` and `PUBSUB_TOPIC_NAME` are correct in the github workflow files
    * [ ] Ensure the `SelfProjectId` and `SelfTopicId` values are correct in appsettings
* [ ] Remove this TODO section in the README to clean up

## Introduction

This solution was generated with [francoishill/Firepuma.Template.GoogleCloudRunService](https://github.com/francoishill/Firepuma.Template.GoogleCloudRunService).

The following projects were generated as part of the solution:

* Firepuma.Scheduling.Domain project contains the domain logic (not tightly coupled to Mongo or other infrastructure specifics)
* Firepuma.Scheduling.Infrastructure contains infrastructure code, like mongo repositories inheriting from `MongoDbRepository<T>`
* Firepuma.Scheduling.Tests contains unit tests
* Firepuma.Scheduling.Worker project contains the service that will get deployed to Google Cloud Run

## Architecture

[MediatR](https://github.com/jbogard/MediatR) is used extensively in this architecture for things like Command, Query and Integration Event handling. Validation and Authorization of Commands and Queries also use MediatR.

The naming of things like `CommandHandler`, `QueryHandler`, `CommandValidator` and `CommandAuthorizer` does not matter but they are merely a good convention.

### Commands

Commands should be used for operations that might write/modify data. They could have Validation and Authorization and their execution events are stored in the database `CommandExecutionEvent` for auditing and DevOps purposes (more on this below in the "Pipeline behaviors for Commands and Queries" section).

An example of a command is `CreatePet`. A command class can inherit from the `BaseCommand` to get an auto generated CommandId and CreatedOn property but it can also just implement from `ICommandRequest` or `ICommandRequest<TResult>`.

Command classes define the payload (properties/input arguments of the command). Each command requires exactly one `CommandHandler` (which implements `IRequestHandler<CommandName, Result>` or `IRequestHandler<CommandName>`). Command handler classes can make use of C# dependency injection and are nested in the Command class.

### Queries

Queries are for operations that do not write/modify data and only read data. They could still have Validation and Authorization but their execution events won't be stored in the database.

A query class should inherit from `BaseQuery<TResult>` and contain a `QueryHandler` class that implements the `IRequestHandler<QueryName, Result>`. Query handler classes can make use of C# dependency injection.

### Validation/Authorization of Commands and Queries

Validation and Authorization classes are nested in the Command/Query class. Example can be seen in `CreatePet`, which contains a `CommandValidator`, `CommandAuthorizer` and `CommandHandler`. A query (like `GetPetsQuery`) can also nest Validator and Authorizer classes.

Validation uses of [FluentValidation](https://docs.fluentvalidation.net/en/latest/) library.

Authorization takes a list of requirements and tests them, `PetNameMustBeAllowedRequirement` is an example. A requirement must extend `IAuthorizationRequirement` and have a `IAuthorizationHandler` which can use dependency injection and should return Succeed or Fail. When authorization requirements fail, it will be stored in `AuthorizationFailureMongoDbEvent` with the exact requirement that failed and a reason.

### Pipeline behaviors for Commands and Queries

A number of `IPipelineBehavior<>` are registered to deal with Commands and Queries:

* `WrapCommandExceptionPipeline` will catch a couple of different types of exceptions and `throw new CommandException`
* `LoggingScopePipeline` just adds logging scope of `CommandRequestType:{Type}`
* `PerformanceLogPipeline` will log debug logs for start/end time and duration of all requests
* `PrerequisitesPipelineBehavior` ensures prerequisite (Validation+Authorization) handlers are executed and pass before executing the DomainRequest (Command/Query)
* `CommandExecutionRecordingPipeline` takes care of storing Command execution events (not Queries) in Mongo
  * Creates an entry when starting and records the result, status and durations afterwards
  * Stores the execution event in-memory on the `ICommandContext` (used later to add Integration Events to the `commandExecutionEvent.ExtraValues` so it is stored before sending the event)

### Integration Events

An example of an integration event is `PetCreated` and it is created+published from the `CreatePet` command handler. Integration events should have a handler extending `ICommandsFactory<TIntegrationEvent>`, that will get automatically executed. The result of the `ICommandsFactory` handler is an array of Commands to be executed (the array can have 0 elements), see `PetCreated.CommandsFactory` for an example.

Integration events can be published from within Handlers of Commands, by injecting `ICommandEventPublisher`. The `_commandEventPublisher.PublishAsync` method arguments expect the command object/payload and integration event payload. Integration events classes need to define attributes to indicate their event type and whether they are and `OutgoingIntegrationEventType` or `IncomingIntegrationEventType` (or both).

Outgoing integration events should extend `BaseOutgoingIntegrationEvent` is is mainly to ensure they get an auto generated `CreatedOn`, `IntegrationEventId` and we define an optional `CommandId`.

Events with only the `OutgoingIntegrationEventType` attribute are those that are published to be handled by external services (perhaps like Notifications)

Events with only the `IncomingIntegrationEventType` attribute are those that are published by external services and handled by this service (perhaps a reply from something like a Notifications or Payments service).

Events with both `OutgoingIntegrationEventType` and `IncomingIntegrationEventType` are events published and handled by this service mainly to create resilience and retries, in case of app shutdown/failure.

Integration events tries to find a CommandExecutionEvent (from the `ICommandContext`) and then adds properties like `IntegrationEventId`, `IntegrationEventPayloadType`, `IntegrationEventPayloadJson` to the `commandExecutionEvent.ExtraValues` dictionary. This will indicate the intent of the command that resulted in integration event, which is going to be published. After publishing the integration event, we will also set properties like `IntegrationEventPublishResultSuccess`, `IntegrationEventPublishResultTime` and `IntegrationEventPublishResultError`, depending on the result of publishing the event. This information is stored on the CommandExecutionEvent so that we can later find events that were not sent out and their originating Command payload.

---

## Deploying

When using github, the deployment will happen automatically due to the folder containing workflow yaml files in the `.github/workflows` folder.

To test locally whether the Dockerfile can build, run the following command:

```shell
docker build --tag tmp-test-firepuma-scheduling-webapi --progress plain --file Firepuma.Scheduling.WebApi/Dockerfile --build-arg version=0.0.0-dev-from-readme .
&& docker run --rm --name tmp-webapi tmp-test-firepuma-scheduling-webapi
```

```shell
docker build --tag tmp-test-firepuma-scheduling-worker --progress plain --file Firepuma.Scheduling.Worker/Dockerfile --build-arg version=0.0.0-dev-from-readme .
&& docker run --rm --name tmp-webapi tmp-test-firepuma-scheduling-worker
```