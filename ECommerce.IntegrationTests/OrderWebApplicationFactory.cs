using Auth.Data;
using Cart.Data;
using ECommerce.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.IntegrationTests;

public class OrderWebApplicationFactory
    : WebApplicationFactory<Order.API.ApiMarker>
{
    public static readonly Guid TestUserId =
        Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public static readonly Guid TestProductId =
        Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration(
    (context, config) =>
    {
        // Load Order.API User Secrets in the test environment.
        config.AddUserSecrets<Order.API.ApiMarker>();
    });

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType ==
                     typeof(DbContextOptions<AppDbContext>));

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(
                    "Server=(localdb)\\MSSQLLocalDB;" +
                    "Database=ECommerce2TestDb;" +
                    "Trusted_Connection=True;" +
                    "TrustServerCertificate=True;");
            });

            var serviceProvider =
                services.BuildServiceProvider();

            using var scope =
                serviceProvider.CreateScope();

            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

            dbContext.Database.Migrate();

            // Find the Customer role.
            var customerRole = dbContext.Roles
                .FirstOrDefault(x => x.Name == "Customer");

            if (customerRole is null)
            {
                throw new InvalidOperationException(
                    "Customer role was not found.");
            }

            // Create the test user if it does not exist.
            var user = dbContext.Users
                .FirstOrDefault(x => x.Id == TestUserId);

            if (user is null)
            {
                dbContext.Users.Add(
                    new User
                    {
                        Id = TestUserId,
                        Name = "Order Integration User",
                        Email = "order-integration@test.com",
                        PasswordHash = "integration-test-hash",
                        RoleId = customerRole.Id,
                        CreatedAtUtc = DateTime.UtcNow,
                        IsDeleted = false
                    });

                dbContext.SaveChanges();
            }

            // Find the test product.
            var product = dbContext.Products
                .FirstOrDefault(x => x.Id == TestProductId);

            if (product is null)
            {
                product = new Product.Data.Product
                {
                    Id = TestProductId,
                    Name = "Integration Test Product",
                    Description = "Test product",
                    Price = 50,
                    Stock = 10,
                    CreatedAtUtc = DateTime.UtcNow,
                    IsDeleted = false
                };

                dbContext.Products.Add(product);
                dbContext.SaveChanges();
            }
            else
            {
                // Reset stock before the tests.
                product.Stock = 10;
                product.IsDeleted = false;

                dbContext.SaveChanges();
            }

            // Find or create the test user's cart.
            var cart = dbContext.Carts
                .FirstOrDefault(x => x.UserId == TestUserId);

            if (cart is null)
            {
                cart = new Cart.Data.Cart
                {
                    Id = Guid.NewGuid(),
                    UserId = TestUserId,
                    CreatedAtUtc = DateTime.UtcNow,
                    IsDeleted = false
                };

                dbContext.Carts.Add(cart);
                dbContext.SaveChanges();
            }
            else
            {
                cart.IsDeleted = false;

                var oldItems = dbContext.CartProducts
                    .Where(x => x.CartId == cart.Id)
                    .ToList();

                dbContext.CartProducts.RemoveRange(oldItems);

                dbContext.SaveChanges();
            }

            // Add a fresh test product to the cart.
            dbContext.CartProducts.Add(
                new CartProduct
                {
                    CartId = cart.Id,
                    ProductId = TestProductId,
                    Quantity = 2
                });

            dbContext.SaveChanges();
        });
    }
}