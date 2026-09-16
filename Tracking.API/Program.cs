using Auth.Services;
using ECommerce.Database;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Tracking.API.Exceptions;
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

// Register the database interface.
builder.Services.AddScoped<ITrackingDbContext>(sp =>
    sp.GetRequiredService<AppDbContext>());

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

// Register controllers and enum serialization.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Register OpenAPI.
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