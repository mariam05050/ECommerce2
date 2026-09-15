namespace Tracking.Data;

public interface ITrackingRepository
{
    Task<Order.Data.Order?> GetOrderByIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<List<TrackingEvent>> GetByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<TrackingEvent?> GetLatestByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    void AddTrackingEvent(
        TrackingEvent trackingEvent);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}