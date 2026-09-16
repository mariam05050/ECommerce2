using Auth.Data;
using ECommerce.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.IntegrationTests;

public class CartWebApplicationFactory
    : WebApplicationFactory<Cart.API.ApiMarker>
{
    public static readonly Guid TestUserId =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration(
    (context, config) =>
    {
        // Load Cart.API User Secrets in the test environment.
        config.AddUserSecrets<Cart.API.ApiMarker>();
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

            // Make sure the Customer role exists.
            var customerRole = dbContext.Roles
                .FirstOrDefault(x => x.Name == "Customer");

            if (customerRole is null)
            {
                throw new InvalidOperationException(
                    "Customer role was not found.");
            }

            // Create the test user if it doesn't exist.
            var testUser = dbContext.Users
                .FirstOrDefault(x => x.Id == TestUserId);

            if (testUser is null)
            {
                dbContext.Users.Add(
                    new User
                    {
                        Id = TestUserId,
                        Name = "Cart Integration User",
                        Email = "cart-integration@test.com",
                        PasswordHash = "integration-test-hash",
                        RoleId = customerRole.Id,
                        CreatedAtUtc = DateTime.UtcNow,
                        IsDeleted = false
                    });

                dbContext.SaveChanges();
            }
        });
    }
}