using Auth.Data;
using Cart.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Order.Data;
using Product.Data;
using Tracking.Data;

namespace ECommerce.Database;

public class AppDbContext : DbContext,
    IAuthDbContext,
    IProductDbContext,
    ICartDbContext,
    IOrderDbContext,
    ITrackingDbContext
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Product.Data.Product> Products => Set<Product.Data.Product>();

    public DbSet<Cart.Data.Cart> Carts => Set<Cart.Data.Cart>();

    public DbSet<CartProduct> CartProducts => Set<CartProduct>();

    public DbSet<Order.Data.Order> Orders => Set<Order.Data.Order>();

    public DbSet<Order.Data.OrderProduct> OrderProducts
        => Set<Order.Data.OrderProduct>();

    public DbSet<Tracking.Data.TrackingEvent> TrackingEvents
        => Set<Tracking.Data.TrackingEvent>();

    public Task<IDbContextTransaction> BeginTransactionAsync(
    CancellationToken cancellationToken = default)
    {
        return Database.BeginTransactionAsync(
            cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(Auth.Data.User).Assembly);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(Product.Data.Product).Assembly);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(Cart.Data.Cart).Assembly);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(Order.Data.Order).Assembly);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(Tracking.Data.TrackingEvent).Assembly);
    }
}