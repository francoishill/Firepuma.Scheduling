using System.ComponentModel.DataAnnotations;
using Firepuma.Scheduling.Domain.Infrastructure.Validation;

// ReSharper disable InvertIf

// ReSharper disable ClassNeverInstantiated.Global

namespace Firepuma.Scheduling.Domain.Features.Scheduling.ValueObjects;

public class ClientApplicationConfigs : Dictionary<string, ClientApplicationConfig>, IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Keys.Count == 0)
        {
            yield return new ValidationResult("At least one client application config is required");
        }

        foreach (var (key, clientAppConfig) in this)
        {
            if (!ValidationHelpers.ValidateDataAnnotations(clientAppConfig, out var clientAppValidationResults))
            {
                foreach (var clientAppValidationResult in clientAppValidationResults)
                {
                    yield return new ValidationResult(
                        $"Validation failed for client app with key '{key}': " + clientAppValidationResult.ErrorMessage,
                        clientAppValidationResult.MemberNames);
                }
            }
        }
    }
}

public class ClientApplicationConfig
{
    [Required]
    public string ApplicationId { get; set; } = null!;

    [Required]
    public string QueueName { get; set; } = null!;
}