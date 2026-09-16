using Cart.Data;
using Cart.Services;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;

namespace ECommerce.UnitTests.CartTests;

public class CartServiceTests
{
    private readonly Mock<ICartRepository> _repositoryMock;
    private readonly Mock<IValidator<AddToCartRequest>> _validatorMock;
    private readonly CartService _service;

    public CartServiceTests()
    {
        _repositoryMock = new Mock<ICartRepository>();
        _validatorMock = new Mock<IValidator<AddToCartRequest>>();

        _service = new CartService(
            _repositoryMock.Object,
            _validatorMock.Object);
    }

    [Fact]
    public async Task GetMyCartAsync_ShouldReturnCart_WhenCartExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var product = new Product.Data.Product
        {
            Id = productId,
            Name = "Keyboard",
            Price = 60,
            IsDeleted = false
        };

        var cart = new Cart.Data.Cart
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IsDeleted = false,
            CartProducts = new List<CartProduct>
            {
                new()
                {
                    CartId = Guid.NewGuid(),
                    ProductId = productId,
                    Quantity = 2,
                    Product = product
                }
            }
        };

        _repositoryMock
            .Setup(x => x.GetByUserIdWithProductsAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        // Act
        var result = await _service.GetMyCartAsync(userId);

        // Assert
        result.Id.Should().Be(cart.Id);
        result.UserId.Should().Be(userId);
        result.Products.Should().HaveCount(1);
        result.Products[0].Name.Should().Be("Keyboard");
        result.Products[0].Price.Should().Be(60);
        result.Products[0].Quantity.Should().Be(2);
    }

    [Fact]
    public async Task GetMyCartAsync_ShouldCreateCart_WhenCartDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var createdCart = new Cart.Data.Cart
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IsDeleted = false,
            CartProducts = new List<CartProduct>()
        };

        _repositoryMock
            .SetupSequence(x => x.GetByUserIdWithProductsAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart.Data.Cart?)null)
            .ReturnsAsync(createdCart);

        // Act
        var result = await _service.GetMyCartAsync(userId);

        // Assert
        result.Id.Should().Be(createdCart.Id);
        result.UserId.Should().Be(userId);
        result.Products.Should().BeEmpty();

        _repositoryMock.Verify(
            x => x.Add(It.IsAny<Cart.Data.Cart>()),
            Times.Once);

        _repositoryMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddProductAsync_ShouldThrowArgumentException_WhenValidationFails()
    {
        // Arrange
        var request = new AddToCartRequest
        {
            ProductId = Guid.NewGuid(),
            Quantity = 0
        };

        var validationResult = new ValidationResult(
            new List<ValidationFailure>
            {
                new(
                    nameof(AddToCartRequest.Quantity),
                    "Quantity must be greater than 0.")
            });

        _validatorMock
            .Setup(x => x.ValidateAsync(
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var act = () => _service.AddProductAsync(
            Guid.NewGuid(),
            request);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentException>();

        _repositoryMock.Verify(
            x => x.GetByUserIdWithProductsAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AddProductAsync_ShouldThrowKeyNotFoundException_WhenProductDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var request = new AddToCartRequest
        {
            ProductId = Guid.NewGuid(),
            Quantity = 2
        };

        _validatorMock
            .Setup(x => x.ValidateAsync(
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var cart = new Cart.Data.Cart
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IsDeleted = false,
            CartProducts = new List<CartProduct>()
        };

        _repositoryMock
            .Setup(x => x.GetByUserIdWithProductsAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _repositoryMock
            .Setup(x => x.GetProductByIdAsync(
                request.ProductId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product.Data.Product?)null);

        // Act
        var act = () => _service.AddProductAsync(
            userId,
            request);

        // Assert
        await act.Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage("Product was not found.");
    }

    [Fact]
    public async Task AddProductAsync_ShouldIncreaseQuantity_WhenProductAlreadyExistsInCart()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var product = new Product.Data.Product
        {
            Id = productId,
            Name = "Keyboard",
            Price = 60,
            IsDeleted = false
        };

        var cart = new Cart.Data.Cart
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IsDeleted = false,
            CartProducts = new List<CartProduct>
            {
                new()
                {
                    CartId = Guid.NewGuid(),
                    ProductId = productId,
                    Quantity = 2,
                    Product = product
                }
            }
        };

        var request = new AddToCartRequest
        {
            ProductId = productId,
            Quantity = 3
        };

        _validatorMock
            .Setup(x => x.ValidateAsync(
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _repositoryMock
            .Setup(x => x.GetByUserIdWithProductsAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _repositoryMock
            .Setup(x => x.GetProductByIdAsync(
                productId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _service.AddProductAsync(
            userId,
            request);

        // Assert
        cart.CartProducts.First().Quantity.Should().Be(5);
        result.Products.Should().HaveCount(1);
        result.Products[0].Quantity.Should().Be(5);

        _repositoryMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddProductAsync_ShouldAddNewProductToCart_WhenProductIsNotAlreadyInCart()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var product = new Product.Data.Product
        {
            Id = productId,
            Name = "Mouse",
            Price = 30,
            IsDeleted = false
        };

        var cart = new Cart.Data.Cart
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IsDeleted = false,
            CartProducts = new List<CartProduct>()
        };

        var request = new AddToCartRequest
        {
            ProductId = productId,
            Quantity = 2
        };

        var updatedCart = new Cart.Data.Cart
        {
            Id = cart.Id,
            UserId = userId,
            IsDeleted = false,
            CartProducts = new List<CartProduct>
        {
            new()
            {
                CartId = cart.Id,
                ProductId = productId,
                Quantity = 2,
                Product = product
            }
        }
        };

        _validatorMock
            .Setup(x => x.ValidateAsync(
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        // First call: get the existing empty cart.
        // Second call: GetMyCartAsync() retrieves the cart after adding the product.
        _repositoryMock
            .SetupSequence(x => x.GetByUserIdWithProductsAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart)
            .ReturnsAsync(updatedCart);

        _repositoryMock
            .Setup(x => x.GetProductByIdAsync(
                productId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _service.AddProductAsync(
            userId,
            request);

        // Assert
        result.Products.Should().HaveCount(1);
        result.Products.First().Name.Should().Be("Mouse");
        result.Products.First().Quantity.Should().Be(2);

        _repositoryMock.Verify(
            x => x.AddCartProduct(
                It.Is<CartProduct>(cp =>
                    cp.ProductId == productId &&
                    cp.Quantity == 2)),
            Times.Once);

        _repositoryMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    
    }

    [Fact]
    public async Task RemoveProductAsync_ShouldThrowKeyNotFoundException_WhenCartDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.GetByUserIdWithProductsAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart.Data.Cart?)null);

        // Act
        var act = () => _service.RemoveProductAsync(
            userId,
            productId);

        // Assert
        await act.Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage("Cart was not found.");
    }

    [Fact]
    public async Task RemoveProductAsync_ShouldRemoveProduct_WhenProductExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var cartProduct = new CartProduct
        {
            CartId = Guid.NewGuid(),
            ProductId = productId,
            Quantity = 2
        };

        var cart = new Cart.Data.Cart
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IsDeleted = false,
            CartProducts = new List<CartProduct>
            {
                cartProduct
            }
        };

        _repositoryMock
            .Setup(x => x.GetByUserIdWithProductsAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        // Act
        await _service.RemoveProductAsync(
            userId,
            productId);

        // Assert
        _repositoryMock.Verify(
            x => x.RemoveCartProduct(cartProduct),
            Times.Once);

        _repositoryMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ClearCartAsync_ShouldRemoveAllProducts()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var product1 = new CartProduct
        {
            CartId = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            Quantity = 1
        };

        var product2 = new CartProduct
        {
            CartId = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            Quantity = 3
        };

        var cart = new Cart.Data.Cart
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IsDeleted = false,
            CartProducts = new List<CartProduct>
            {
                product1,
                product2
            }
        };

        _repositoryMock
            .Setup(x => x.GetByUserIdWithProductsAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        // Act
        await _service.ClearCartAsync(userId);

        // Assert
        _repositoryMock.Verify(
            x => x.RemoveCartProduct(product1),
            Times.Once);

        _repositoryMock.Verify(
            x => x.RemoveCartProduct(product2),
            Times.Once);

        _repositoryMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}