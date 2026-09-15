namespace Tracking.Data;

public class TrackingEvent
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public TrackingStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}