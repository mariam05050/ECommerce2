using Tracking.Data;

namespace Tracking.Services;

public class TrackingService : ITrackingService
{
    private readonly ITrackingRepository _repository;

    public TrackingService(ITrackingRepository repository)
    {
        _repository = repository;
    }

    public async Task<TrackingResponse> GetTrackingAsync(
        Guid userId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await _repository.GetOrderByIdAsync(
            orderId,
            cancellationToken);

        if (order is null || order.UserId != userId)
        {
            throw new KeyNotFoundException(
                "Order was not found.");
        }

        var events = await _repository.GetByOrderIdAsync(
            orderId,
            cancellationToken);

        return new TrackingResponse
        {
            OrderId = orderId,
            Events = events
                .Select(trackingEvent =>
                    new TrackingEventResponse
                    {
                        Id = trackingEvent.Id,
                        OrderId = trackingEvent.OrderId,
                        Status = trackingEvent.Status,
                        CreatedAtUtc =
                            trackingEvent.CreatedAtUtc
                    })
                .ToList()
        };
    }

    public async Task<TrackingEventResponse> UpdateStatusAsync(
        Guid orderId,
        TrackingStatus status,
        CancellationToken cancellationToken = default)
    {
        var order = await _repository.GetOrderByIdAsync(
            orderId,
            cancellationToken);

        if (order is null || order.IsDeleted)
        {
            throw new KeyNotFoundException(
                "Order was not found.");
        }

        var latestEvent =
            await _repository.GetLatestByOrderIdAsync(
                orderId,
                cancellationToken);

        if (latestEvent is not null &&
            latestEvent.Status == status)
        {
            throw new InvalidOperationException(
                "The order already has this status.");
        }

        var trackingEvent = new TrackingEvent
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Status = status,
            CreatedAtUtc = DateTime.UtcNow
        };

        _repository.AddTrackingEvent(trackingEvent);

        await _repository.SaveChangesAsync(
            cancellationToken);

        return new TrackingEventResponse
        {
            Id = trackingEvent.Id,
            OrderId = trackingEvent.OrderId,
            Status = trackingEvent.Status,
            CreatedAtUtc = trackingEvent.CreatedAtUtc
        };
    }
}