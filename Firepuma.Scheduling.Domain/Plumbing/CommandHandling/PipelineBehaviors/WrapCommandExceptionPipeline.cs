using System.Net;
using Firepuma.CommandsAndQueries.Abstractions.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Firepuma.Scheduling.Domain.Plumbing.CommandHandling.PipelineBehaviors;

public class WrapCommandExceptionPipeline<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<WrapCommandExceptionPipeline<TRequest, TResponse>> _logger;

    public WrapCommandExceptionPipeline(
        ILogger<WrapCommandExceptionPipeline<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestType = typeof(TRequest);
        var requestTypeName = requestType.FullName ?? requestType.Name;

        try
        {
            return await next();
        }
        catch (ValidationException validationException)
        {
            _logger.LogWarning(
                validationException,
                "Failed to perform request {RequestType}, validation error was: {Error}",
                requestTypeName, validationException.Message);

            throw new CommandException(
                HttpStatusCode.BadRequest,
                validationException,
                validationException
                    .Errors.Select(e => new CommandException.Error
                    {
                        Code = HttpStatusCode.BadRequest.ToString(),
                        Message = e.ErrorMessage,
                    }).ToArray());
        }
        catch (AuthorizationException authorizationException)
        {
            _logger.LogWarning(
                authorizationException,
                "Failed to perform request {RequestType}, authorization error was: {Error}",
                requestTypeName, authorizationException.Message);

            throw new CommandException(
                HttpStatusCode.Forbidden,
                authorizationException,
                new CommandException.Error
                {
                    Code = HttpStatusCode.Forbidden.ToString(),
                    Message = authorizationException.Message,
                });
        }
        catch (PreconditionFailedException preconditionFailedException)
        {
            _logger.LogWarning(
                preconditionFailedException,
                "Failed to perform request {RequestType}, precondition failed error was: {Error}",
                requestTypeName, preconditionFailedException.Message);

            throw new CommandException(
                HttpStatusCode.PreconditionFailed,
                preconditionFailedException,
                new CommandException.Error
                {
                    Code = HttpStatusCode.PreconditionFailed.ToString(),
                    Message = preconditionFailedException.Message,
                });
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to perform request {RequestType}, error was: {Error}",
                requestTypeName, exception.Message);

            throw;
        }
    }
}