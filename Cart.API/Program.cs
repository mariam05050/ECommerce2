using Auth.Services;
using Cart.API.Exceptions;
using Cart.Data;
using Cart.Services;
using ECommerce.Database;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Register database.
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "DefaultConnection"));
});

// Register cart database interface.
builder.Services.AddScoped<ICartDbContext>(sp =>
    sp.GetRequiredService<AppDbContext>());

// Register cart services.
builder.Services.AddScoped<
    ICartService,
    CartService>();

builder.Services.AddScoped<
    ICartRepository,
    CartRepository>();

// Register validation.
builder.Services.AddValidatorsFromAssemblyContaining<
    AddToCartValidator>();

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