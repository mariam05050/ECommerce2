using Microsoft.EntityFrameworkCore;

namespace Cart.Data;

public interface ICartDbContext
{
    DbSet<Cart> Carts { get; }

    DbSet<CartProduct> CartProducts { get; }

    DbSet<Product.Data.Product> Products { get; }

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}