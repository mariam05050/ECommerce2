using Cart.Data;
using FluentValidation;

namespace Cart.Services;

public class CartService : ICartService
{
    private readonly ICartRepository _repository;
    private readonly IValidator<AddToCartRequest>
        _addToCartValidator;

    public CartService(
        ICartRepository repository,
        IValidator<AddToCartRequest> addToCartValidator)
    {
        _repository = repository;
        _addToCartValidator = addToCartValidator;
    }

    public async Task<CartResponse> GetMyCartAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var cart = await _repository.GetByUserIdWithProductsAsync(
            userId,
            cancellationToken);

        if (cart is null)
        {
            cart = new Cart.Data.Cart
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedAtUtc = DateTime.UtcNow,
                IsDeleted = false
            };

            _repository.Add(cart);

            await _repository.SaveChangesAsync(
                cancellationToken);

            cart = await _repository
                .GetByUserIdWithProductsAsync(
                    userId,
                    cancellationToken);
        }

        return new CartResponse
        {
            Id = cart!.Id,
            UserId = cart.UserId,

            Products = cart.CartProducts
                .Select(x => new ProductInCartResponse
                {
                    Id = x.Product.Id,
                    Name = x.Product.Name,
                    Price = x.Product.Price,
                    Quantity = x.Quantity
                })
                .ToList()
        };
    }

    public async Task<CartResponse> AddProductAsync(
        Guid userId,
        AddToCartRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult =
            await _addToCartValidator.ValidateAsync(
                request,
                cancellationToken);

        if (!validationResult.IsValid)
        {
            throw new ArgumentException(
                validationResult.ToString());
        }

        var cart = await _repository
            .GetByUserIdWithProductsAsync(
                userId,
                cancellationToken);

        if (cart is null)
        {
            cart = new Cart.Data.Cart
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedAtUtc = DateTime.UtcNow,
                IsDeleted = false
            };

            _repository.Add(cart);

            await _repository.SaveChangesAsync(
                cancellationToken);

            cart = await _repository
                .GetByUserIdWithProductsAsync(
                    userId,
                    cancellationToken);
        }

        var product = await _repository.GetProductByIdAsync(
            request.ProductId,
            cancellationToken);

        if (product is null || product.IsDeleted)
        {
            throw new KeyNotFoundException(
                "Product was not found.");
        }

        var existingCartProduct = cart!.CartProducts
            .FirstOrDefault(
                x => x.ProductId == request.ProductId);

        if (existingCartProduct is not null)
        {
            existingCartProduct.Quantity += request.Quantity;

            await _repository.SaveChangesAsync(
                cancellationToken);

            return await GetMyCartAsync(
                userId,
                cancellationToken);
        }

        var cartProduct = new CartProduct
        {
            CartId = cart.Id,
            ProductId = product.Id,
            Quantity = request.Quantity
        };

        _repository.AddCartProduct(cartProduct);

        await _repository.SaveChangesAsync(
            cancellationToken);

        return await GetMyCartAsync(
            userId,
            cancellationToken);
    }

    public async Task RemoveProductAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var cart = await _repository
            .GetByUserIdWithProductsAsync(
                userId,
                cancellationToken);

        if (cart is null)
        {
            throw new KeyNotFoundException(
                "Cart was not found.");
        }

        var cartProduct = cart.CartProducts
            .FirstOrDefault(
                x => x.ProductId == productId);

        if (cartProduct is null)
        {
            throw new KeyNotFoundException(
                "Product was not found in the cart.");
        }

        _repository.RemoveCartProduct(cartProduct);

        await _repository.SaveChangesAsync(
            cancellationToken);
    }

    public async Task ClearCartAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var cart = await _repository
            .GetByUserIdWithProductsAsync(
                userId,
                cancellationToken);

        if (cart is null)
        {
            throw new KeyNotFoundException(
                "Cart was not found.");
        }

        foreach (var cartProduct in cart.CartProducts)
        {
            _repository.RemoveCartProduct(cartProduct);
        }

        await _repository.SaveChangesAsync(
            cancellationToken);
    }
}