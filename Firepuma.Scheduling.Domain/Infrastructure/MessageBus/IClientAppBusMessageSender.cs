using Firepuma.Scheduling.Domain.Features.Scheduling.ValueObjects;
using Firepuma.Scheduling.Domain.Infrastructure.MessageBus.BusMessages;
using Firepuma.Scheduling.Domain.Infrastructure.MessageBus.Results;

namespace Firepuma.Scheduling.Domain.Infrastructure.MessageBus;

public interface IClientAppBusMessageSender
{
    Task<SentMessageToApplicationResult> SendMessageToApplicationAsync(
        ClientApplicationId applicationId,
        string correlationId,
        ISchedulingBusMessage messageDto,
        CancellationToken cancellationToken);
}