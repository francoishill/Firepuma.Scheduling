using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Firepuma.Scheduling.Domain.Infrastructure.Validation;
using Newtonsoft.Json.Linq;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Firepuma.Scheduling.FunctionApp.Api.HttpTriggers.Requests;

public class AddScheduledJobRequest
{
    [Required]
    [MinLength(2)]
    public string ApplicationId { get; set; }

    [Required]
    public bool? IsRecurring { get; set; }

    public DateTime? StartTime { get; set; }

    public int? RecurringUtcOffsetInMinutes { get; set; }
    public string RecurringCronExpression { get; set; }

    [Required]
    public JObject ExtraValues { get; set; }

    public bool Validate(out List<ValidationResult> validationResults)
    {
        if (!ValidationHelpers.ValidateDataAnnotations(this, out validationResults))
        {
            return false;
        }

        if (IsRecurring == true && RecurringUtcOffsetInMinutes == null)
        {
            validationResults = new List<ValidationResult>
            {
                new($"The {nameof(RecurringUtcOffsetInMinutes)} field is required"),
            };
            return false;
        }

        if (IsRecurring == true && string.IsNullOrWhiteSpace(RecurringCronExpression))
        {
            validationResults = new List<ValidationResult>
            {
                new($"The {nameof(RecurringCronExpression)} field is required"),
            };
            return false;
        }

        if (ExtraValues == null || !ExtraValues.Properties().Any())
        {
            validationResults = new List<ValidationResult>
            {
                new($"The {nameof(ExtraValues)} field must be and object with at least one property"),
            };
            return false;
        }

        return true;
    }
}