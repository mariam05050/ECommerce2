using Auth.API.Exceptions;
using Auth.Data;
using Auth.Services;
using Cart.Data;
using Cart.Services;
using ECommerce.Database;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Order.Data;
using Order.Services;
using Product.Data;
using Product.Services;
using Scalar.AspNetCore;
using System.Text;
using Tracking.Data;
using Tracking.Services;

var builder = WebApplication.CreateBuilder(args);

// Register the shared database context.
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Expose the context through each module's interface.
builder.Services.AddScoped<IAuthDbContext>(sp =>
    sp.GetRequiredService<AppDbContext>());

builder.Services.AddScoped<IProductDbContext>(sp =>
    sp.GetRequiredService<AppDbContext>());

builder.Services.AddScoped<ICartDbContext>(sp =>
    sp.GetRequiredService<AppDbContext>());

builder.Services.AddScoped<IOrderDbContext>(sp =>
    sp.GetRequiredService<AppDbContext>());

builder.Services.AddScoped<ITrackingDbContext>(sp =>
    sp.GetRequiredService<AppDbContext>());

// Register AutoMapper profiles.
builder.Services.AddAutoMapper(
    cfg => { },
    typeof(Auth.Services.MappingProfile),
    typeof(Product.Services.MappingProfile));

// Bind JWT configuration from appsettings.json.
builder.Services
    .AddOptions<JwtSettings>()
    .Bind(builder.Configuration.GetSection("Jwt"))
    .ValidateOnStart();

// Register authentication services.
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();

// Register product services.
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// Register cart services.
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ICartRepository, CartRepository>();

// Register order services.
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// Register tracking services.
builder.Services.AddScoped<ITrackingService, TrackingService>();
builder.Services.AddScoped<ITrackingRepository, TrackingRepository>();

// Convert exceptions into API responses.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Register validators.
builder.Services.AddValidatorsFromAssemblyContaining<RegisterValidator>();

builder.Services.AddValidatorsFromAssemblyContaining<
    Product.Services.CreateProductValidator>();

builder.Services.AddValidatorsFromAssemblyContaining<
    Cart.Services.AddToCartValidator>();

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
                        Encoding.UTF8.GetBytes(jwtSettings.Key))
            };
    });

// Register controllers and serialize enums as strings.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Configure OpenAPI and JWT security.
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer(
        (document, context, cancellationToken) =>
        {
            document.Components ??= new OpenApiComponents();

            document.Components.SecuritySchemes ??=
                new Dictionary<string, IOpenApiSecurityScheme>();

            document.Components.SecuritySchemes["Bearer"] =
                new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    In = ParameterLocation.Header,
                    BearerFormat = "JWT"
                };

            return Task.CompletedTask;
        });

    options.AddOperationTransformer(
        (operation, context, cancellationToken) =>
        {
            var hasAuthorize = context.Description
                .ActionDescriptor
                .EndpointMetadata
                .OfType<AuthorizeAttribute>()
                .Any();

            if (hasAuthorize)
            {
                operation.Security ??= [];

                operation.Security.Add(
                    new OpenApiSecurityRequirement
                    {
                        [
                            new OpenApiSecuritySchemeReference(
                                "Bearer",
                                context.Document)
                        ] = []
                    });
            }

            return Task.CompletedTask;
        });
});

var app = builder.Build();

// Handle exceptions before reaching the endpoints.
app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapOpenApi();

app.MapScalarApiReference();

app.MapControllers();

app.Run();

public partial class Program
{
}