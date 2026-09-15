using Microsoft.EntityFrameworkCore;

namespace Tracking.Data;

public class TrackingRepository : ITrackingRepository
{
    private readonly ITrackingDbContext _context;

    public TrackingRepository(ITrackingDbContext context)
    {
        _context = context;
    }

    public async Task<Order.Data.Order?> GetOrderByIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .FirstOrDefaultAsync(
                x => x.Id == orderId,
                cancellationToken);
    }

    public async Task<List<TrackingEvent>> GetByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return await _context.TrackingEvents
            .Where(x => x.OrderId == orderId)
            .AsNoTracking()
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<TrackingEvent?> GetLatestByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return await _context.TrackingEvents
            .Where(x => x.OrderId == orderId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public void AddTrackingEvent(
        TrackingEvent trackingEvent)
    {
        _context.TrackingEvents.Add(trackingEvent);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}