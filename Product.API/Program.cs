using ECommerce.Database;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Product.API.Exceptions;
using Product.Data;
using Product.Services;

var builder = WebApplication.CreateBuilder(args);

// Register the database.
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "DefaultConnection"));
});

// Register the database interface.
builder.Services.AddScoped<IProductDbContext>(sp =>
    sp.GetRequiredService<AppDbContext>());

// Register AutoMapper.
builder.Services.AddAutoMapper(
    cfg => { },
    typeof(Product.Services.MappingProfile));

// Register product services.
builder.Services.AddScoped<
    IProductService,
    ProductService>();

builder.Services.AddScoped<
    IProductRepository,
    ProductRepository>();

// Register validation.
builder.Services.AddValidatorsFromAssemblyContaining<
    CreateProductValidator>();

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

app.UseAuthorization();

app.MapOpenApi();
app.MapControllers();

app.Run();

public partial class Program
{
}