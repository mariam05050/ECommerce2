using Microsoft.EntityFrameworkCore;

namespace Tracking.Data;

public interface ITrackingDbContext
{
    DbSet<TrackingEvent> TrackingEvents { get; }

    DbSet<Order.Data.Order> Orders { get; }

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}