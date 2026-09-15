using Microsoft.EntityFrameworkCore;

namespace Order.Data;

public interface IOrderDbContext
{
    DbSet<Order> Orders { get; }

    DbSet<OrderProduct> OrderProducts { get; }

    DbSet<Cart.Data.Cart> Carts { get; }

    DbSet<Cart.Data.CartProduct> CartProducts { get; }

    DbSet<Product.Data.Product> Products { get; }

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}