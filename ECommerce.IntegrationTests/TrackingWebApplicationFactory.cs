using Auth.Data;
using ECommerce.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tracking.Data;

namespace ECommerce.IntegrationTests;

public class TrackingWebApplicationFactory
    : WebApplicationFactory<Tracking.API.ApiMarker>
{
    public static readonly Guid TestUserId =
        Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    public static readonly Guid TestOrderId =
        Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration(
    (context, config) =>
    {
        // Load Tracking.API User Secrets in the test environment.
        config.AddUserSecrets<Tracking.API.ApiMarker>();
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

            // Create the test customer.
            var user = dbContext.Users
                .FirstOrDefault(x => x.Id == TestUserId);

            if (user is null)
            {
                dbContext.Users.Add(
                    new User
                    {
                        Id = TestUserId,
                        Name = "Tracking Integration User",
                        Email = "tracking-integration@test.com",
                        PasswordHash = "integration-test-hash",
                        RoleId = customerRole.Id,
                        CreatedAtUtc = DateTime.UtcNow,
                        IsDeleted = false
                    });

                dbContext.SaveChanges();
            }

            // Create the test order.
            var order = dbContext.Orders
                .FirstOrDefault(x => x.Id == TestOrderId);

            if (order is null)
            {
                dbContext.Orders.Add(
                    new Order.Data.Order
                    {
                        Id = TestOrderId,
                        UserId = TestUserId,
                        Total = 100,
                        CreatedAtUtc = DateTime.UtcNow,
                        IsDeleted = false
                    });

                dbContext.SaveChanges();
            }

            // Reset tracking history before each test run.
            var oldTrackingEvents = dbContext.TrackingEvents
                .Where(x => x.OrderId == TestOrderId)
                .ToList();

            dbContext.TrackingEvents.RemoveRange(oldTrackingEvents);

            dbContext.SaveChanges();

            dbContext.TrackingEvents.Add(
                new TrackingEvent
                {
                    Id = Guid.NewGuid(),
                    OrderId = TestOrderId,
                    Status = TrackingStatus.Processing,
                    CreatedAtUtc = DateTime.UtcNow
                });

            dbContext.SaveChanges();

        });
    }
}