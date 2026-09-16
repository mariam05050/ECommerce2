using Auth.Services;
using ECommerce.Database;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Order.API.Exceptions;
using Order.Data;
using Order.Services;
using System.Text;
using Tracking.Data;
using Tracking.Services;

var builder = WebApplication.CreateBuilder(args);

// Register the database.
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "DefaultConnection"));
});

// Register database interfaces.
builder.Services.AddScoped<IOrderDbContext>(sp =>
    sp.GetRequiredService<AppDbContext>());

builder.Services.AddScoped<ITrackingDbContext>(sp =>
    sp.GetRequiredService<AppDbContext>());

// Register order services.
builder.Services.AddScoped<
    IOrderService,
    OrderService>();

builder.Services.AddScoped<
    IOrderRepository,
    OrderRepository>();

// Register tracking services.
builder.Services.AddScoped<
    ITrackingService,
    TrackingService>();

builder.Services.AddScoped<
    ITrackingRepository,
    TrackingRepository>();

// Configure JWT authentication.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration
            .GetSection("Jwt")
            .Get<JwtSettings>()!;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,

                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,

                ValidateLifetime = true,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            jwtSettings.Key))
            };
    });

// Register exception handling.
builder.Services.AddExceptionHandler<
    GlobalExceptionHandler>();

builder.Services.AddProblemDetails();

// Register controllers and OpenAPI.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Handle exceptions globally.
app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapControllers();

app.Run();

public partial class Program
{
}