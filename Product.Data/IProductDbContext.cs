using Microsoft.EntityFrameworkCore;

namespace Product.Data;

public interface IProductDbContext
{
    DbSet<Product> Products { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}