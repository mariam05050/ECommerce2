namespace Tracking.Services;

public interface ITrackingService
{
    Task<TrackingResponse> GetTrackingAsync(
        Guid userId,
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<TrackingEventResponse> UpdateStatusAsync(
        Guid orderId,
        Tracking.Data.TrackingStatus status,
        CancellationToken cancellationToken = default);
}