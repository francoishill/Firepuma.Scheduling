using System.ComponentModel.DataAnnotations;

// ReSharper disable ClassNeverInstantiated.Global

namespace Firepuma.Scheduling.FunctionApp.Config;

public class ClientApplicationConfig
{
    [Required]
    public string ApplicationId { get; set; }

    [Required]
    public string QueueName { get; set; }
}