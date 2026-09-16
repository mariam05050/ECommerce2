using Auth.API;
using ECommerce.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.IntegrationTests;

public class CustomWebApplicationFactory
    : WebApplicationFactory<ApiMarker>
{
    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration(
    (context, config) =>
    {
        // Load Auth.API User Secrets in the test environment.
        config.AddUserSecrets<Auth.API.ApiMarker>();
    });

        builder.ConfigureServices(services =>
        {
            // Remove the normal AppDbContext options.
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(
                    DbContextOptions<AppDbContext>));

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            // Use a separate database for integration tests.
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(
                    "Server=(localdb)\\MSSQLLocalDB;" +
                    "Database=ECommerce2TestDb;" +
                    "Trusted_Connection=True;" +
                    "TrustServerCertificate=True;");
            });

            // Create the test database and apply migrations.
            var serviceProvider = services.BuildServiceProvider();

            using var scope = serviceProvider.CreateScope();

            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

            dbContext.Database.Migrate();
        });
    }
}