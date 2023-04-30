using System.Reflection;
using AutoMapper;
using Firepuma.Scheduling.Worker.Admin;

namespace Firepuma.Scheduling.Tests.AutoMapping;

public class AutoMapperConfigurationTests
{
    [Fact]
    public void WhenProfilesAreConfigured_ItShouldNotThrowException()
    {
        // Arrange
        var config = new MapperConfiguration(configuration =>
        {
            //Uncomment this if we ever add mapping of Enums
            // configuration.EnableEnumMappingValidation();

            configuration.AddMaps(typeof(ManualHealthCheckingController).GetTypeInfo().Assembly);
        });

        // Assert
        config.AssertConfigurationIsValid();
    }
}