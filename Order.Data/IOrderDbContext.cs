using Cart.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Product.Data;

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

    Task<IDbContextTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default);
}