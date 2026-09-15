namespace Tracking.Services;

public class TrackingEventResponse
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Tracking.Data.TrackingStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}

public class TrackingResponse
{
    public Guid OrderId { get; set; }

    public List<TrackingEventResponse> Events { get; set; }
        = new List<TrackingEventResponse>();
}