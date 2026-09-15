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

// =====================================================
// Database
// =====================================================

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"));
});


// =====================================================
// DbContext Interfaces
// =====================================================

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


// =====================================================
// AutoMapper
// =====================================================

builder.Services.AddAutoMapper(
    cfg => { },
    typeof(Auth.Services.MappingProfile),
    typeof(Product.Services.MappingProfile));


// =====================================================
// Options
// =====================================================

builder.Services
    .AddOptions<JwtSettings>()
    .Bind(builder.Configuration.GetSection("Jwt"))
    .ValidateOnStart();


// =====================================================
// Auth Services
// =====================================================

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();


// =====================================================
// Product Services
// =====================================================

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();


// =====================================================
// Cart Services
// =====================================================

builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ICartRepository, CartRepository>();


// =====================================================
// Order Services


builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

//Tracking service
builder.Services.AddScoped<ITrackingService, TrackingService>();
builder.Services.AddScoped<ITrackingRepository, TrackingRepository>();


// =====================================================
// FluentValidation
// =====================================================

builder.Services.AddValidatorsFromAssemblyContaining<RegisterValidator>();

builder.Services.AddValidatorsFromAssemblyContaining<
    Product.Services.CreateProductValidator>();

builder.Services.AddValidatorsFromAssemblyContaining<
    Cart.Services.AddToCartValidator>();


// =====================================================
// Authentication / JWT
// =====================================================

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


// =====================================================
// Controllers
// =====================================================

builder.Services.AddControllers();


// =====================================================
// OpenAPI / Scalar
// =====================================================

builder.Services.AddOpenApi(options =>
{
    // Register JWT Bearer security scheme.
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

    // Mark endpoints using [Authorize] as requiring JWT.
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


// =====================================================
// Build Application
// =====================================================

var app = builder.Build();


// =====================================================
// Middleware
// =====================================================

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();


// =====================================================
// Endpoints
// =====================================================

app.MapOpenApi();

app.MapScalarApiReference();

app.MapControllers();

app.Run();