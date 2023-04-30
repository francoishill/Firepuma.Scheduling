using Firepuma.Scheduling.Infrastructure.Plumbing.IntegrationEvents.Services;

namespace Firepuma.Scheduling.Tests.IntegrationEvents;

public class GooglePubSubIntegrationEventTransportTests
{
    [Theory]
    [InlineData("googlepubsub:proj/app", "proj", "app")]
    [InlineData("googlepubsub:my-1-project/my-1-app", "my-1-project", "my-1-app")]
    [InlineData("googlepubsub:my-project-1/my-app-1", "my-project-1", "my-app-1")]
    public void ParseTopicNameFromEventReplyToAddress_Valid_cases(
        string eventReplyToAddress,
        string expectedProjectId,
        string expectedTopicId)
    {
        // Arrange
        // Act
        var parsed = GooglePubSubIntegrationEventTransport.TryParseTopicNameFromEventReplyToAddress(
            eventReplyToAddress,
            out var topicName);

        // Assert
        Assert.True(parsed);
        Assert.NotNull(topicName);
        Assert.Equal(expectedProjectId, topicName.ProjectId);
        Assert.Equal(expectedTopicId, topicName.TopicId);
    }

    [Theory]
    [InlineData("blah:proj/app")]
    [InlineData("googlepubsub:proj/1app")]
    [InlineData("googlepubsub:1proj/app")]
    public void ParseTopicNameFromEventReplyToAddress_Invalid_cases(string eventReplyToAddress)
    {
        // Arrange
        // Act
        var parsed = GooglePubSubIntegrationEventTransport.TryParseTopicNameFromEventReplyToAddress(
            eventReplyToAddress,
            out var topicName);

        // Assert
        Assert.False(parsed);
        Assert.Null(topicName);
    }
}