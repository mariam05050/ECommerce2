using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Order.Data;
using Order.Services;
using Tracking.Data;
using Tracking.Services;

namespace ECommerce.UnitTests.OrderTests;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _repositoryMock;
    private readonly Mock<ITrackingService> _trackingServiceMock;
    private readonly Mock<IDbContextTransaction> _transactionMock;
    private readonly OrderService _service;

    public OrderServiceTests()
    {
        _repositoryMock = new Mock<IOrderRepository>();
        _trackingServiceMock = new Mock<ITrackingService>();
        _transactionMock = new Mock<IDbContextTransaction>();

        _repositoryMock
            .Setup(x => x.BeginTransactionAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        _service = new OrderService(
            _repositoryMock.Object,
            _trackingServiceMock.Object);
    }

    [Fact]
    public async Task CheckoutAsync_ShouldThrowKeyNotFoundException_WhenCartDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.GetCartByUserIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cart.Data.Cart?)null);

        // Act
        var act = () => _service.CheckoutAsync(userId);

        // Assert
        await act.Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage("Cart was not found.");

        _repositoryMock.Verify(
            x => x.RollbackTransactionAsync(
                _transactionMock.Object,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _repositoryMock.Verify(
            x => x.AddOrder(It.IsAny<Order.Data.Order>()),
            Times.Never);
    }

    [Fact]
    public async Task CheckoutAsync_ShouldThrowInvalidOperationException_WhenCartIsEmpty()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var cart = new Cart.Data.Cart
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CartProducts = new List<Cart.Data.CartProduct>()
        };

        _repositoryMock
            .Setup(x => x.GetCartByUserIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        // Act
        var act = () => _service.CheckoutAsync(userId);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Cart is empty.");

        _repositoryMock.Verify(
            x => x.RollbackTransactionAsync(
                _transactionMock.Object,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckoutAsync_ShouldThrowKeyNotFoundException_WhenProductDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var cartProduct = new Cart.Data.CartProduct
        {
            ProductId = Guid.NewGuid(),
            Quantity = 1,
            Product = null!
        };

        var cart = new Cart.Data.Cart
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CartProducts = new List<Cart.Data.CartProduct>
            {
                cartProduct
            }
        };

        _repositoryMock
            .Setup(x => x.GetCartByUserIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        // Act
        var act = () => _service.CheckoutAsync(userId);

        // Assert
        await act.Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage("A product in the cart was not found.");

        _repositoryMock.Verify(
            x => x.RollbackTransactionAsync(
                _transactionMock.Object,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckoutAsync_ShouldThrowInvalidOperationException_WhenQuantityIsInvalid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var product = new Product.Data.Product
        {
            Id = productId,
            Name = "Keyboard",
            Price = 60,
            Stock = 10,
            IsDeleted = false
        };

        var cart = new Cart.Data.Cart
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CartProducts = new List<Cart.Data.CartProduct>
            {
                new()
                {
                    ProductId = productId,
                    Quantity = 0,
                    Product = product
                }
            }
        };

        _repositoryMock
            .Setup(x => x.GetCartByUserIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        // Act
        var act = () => _service.CheckoutAsync(userId);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Cart contains an invalid quantity.");

        _repositoryMock.Verify(
            x => x.RollbackTransactionAsync(
                _transactionMock.Object,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckoutAsync_ShouldThrowInvalidOperationException_WhenStockIsInsufficient()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var product = new Product.Data.Product
        {
            Id = productId,
            Name = "Keyboard",
            Price = 60,
            Stock = 2,
            IsDeleted = false
        };

        var cart = new Cart.Data.Cart
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CartProducts = new List<Cart.Data.CartProduct>
            {
                new()
                {
                    ProductId = productId,
                    Quantity = 5,
                    Product = product
                }
            }
        };

        _repositoryMock
            .Setup(x => x.GetCartByUserIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        // Act
        var act = () => _service.CheckoutAsync(userId);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage(
                "Not enough stock for product 'Keyboard'.");

        _repositoryMock.Verify(
            x => x.RollbackTransactionAsync(
                _transactionMock.Object,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _repositoryMock.Verify(
            x => x.AddOrder(It.IsAny<Order.Data.Order>()),
            Times.Never);
    }

    [Fact]
    public async Task CheckoutAsync_ShouldCreateOrderAndReduceStock_WhenCheckoutIsValid()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var product1 = new Product.Data.Product
        {
            Id = Guid.NewGuid(),
            Name = "Keyboard",
            Price = 60,
            Stock = 10,
            IsDeleted = false
        };

        var product2 = new Product.Data.Product
        {
            Id = Guid.NewGuid(),
            Name = "Mouse",
            Price = 30,
            Stock = 5,
            IsDeleted = false
        };

        var cart = new Cart.Data.Cart
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CartProducts = new List<Cart.Data.CartProduct>
            {
                new()
                {
                    ProductId = product1.Id,
                    Quantity = 2,
                    Product = product1
                },
                new()
                {
                    ProductId = product2.Id,
                    Quantity = 1,
                    Product = product2
                }
            }
        };

        _repositoryMock
            .Setup(x => x.GetCartByUserIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _repositoryMock
            .Setup(x => x.GetOrderProductsByOrderIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OrderProduct>());

        _trackingServiceMock
            .Setup(x => x.UpdateStatusAsync(
                It.IsAny<Guid>(),
                TrackingStatus.Processing,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TrackingEventResponse());

        // Act
        var result = await _service.CheckoutAsync(userId);

        // Assert
        result.Should().NotBeNull();

        result.UserId.Should().Be(userId);
        result.Total.Should().Be(150);

        product1.Stock.Should().Be(8);
        product2.Stock.Should().Be(4);

        _repositoryMock.Verify(
            x => x.AddOrder(
                It.Is<Order.Data.Order>(order =>
                    order.UserId == userId &&
                    order.Total == 150)),
            Times.Once);

        _repositoryMock.Verify(
            x => x.AddOrderProduct(
                It.Is<OrderProduct>(item =>
                    item.ProductId == product1.Id &&
                    item.Quantity == 2 &&
                    item.UnitPrice == 60)),
            Times.Once);

        _repositoryMock.Verify(
            x => x.AddOrderProduct(
                It.Is<OrderProduct>(item =>
                    item.ProductId == product2.Id &&
                    item.Quantity == 1 &&
                    item.UnitPrice == 30)),
            Times.Once);

        _repositoryMock.Verify(
            x => x.RemoveCartProduct(
                It.IsAny<Cart.Data.CartProduct>()),
            Times.Exactly(2));

        _repositoryMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

        _trackingServiceMock.Verify(
            x => x.UpdateStatusAsync(
                It.IsAny<Guid>(),
                TrackingStatus.Processing,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _repositoryMock.Verify(
            x => x.CommitTransactionAsync(
                _transactionMock.Object,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _repositoryMock.Verify(
            x => x.RollbackTransactionAsync(
                It.IsAny<IDbContextTransaction>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CheckoutAsync_ShouldSaveProcessingTrackingStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var product = new Product.Data.Product
        {
            Id = Guid.NewGuid(),
            Name = "USB-C Cable",
            Price = 10,
            Stock = 10,
            IsDeleted = false
        };

        var cart = new Cart.Data.Cart
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CartProducts = new List<Cart.Data.CartProduct>
            {
                new()
                {
                    ProductId = product.Id,
                    Quantity = 2,
                    Product = product
                }
            }
        };

        _repositoryMock
            .Setup(x => x.GetCartByUserIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _repositoryMock
            .Setup(x => x.GetOrderProductsByOrderIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OrderProduct>());

        _trackingServiceMock
            .Setup(x => x.UpdateStatusAsync(
                It.IsAny<Guid>(),
                TrackingStatus.Processing,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TrackingEventResponse());

        // Act
        await _service.CheckoutAsync(userId);

        // Assert
        _trackingServiceMock.Verify(
            x => x.UpdateStatusAsync(
                It.IsAny<Guid>(),
                TrackingStatus.Processing,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckoutAsync_ShouldRollback_WhenTrackingCreationFails()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var product = new Product.Data.Product
        {
            Id = Guid.NewGuid(),
            Name = "Keyboard",
            Price = 60,
            Stock = 10,
            IsDeleted = false
        };

        var cart = new Cart.Data.Cart
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CartProducts = new List<Cart.Data.CartProduct>
            {
                new()
                {
                    ProductId = product.Id,
                    Quantity = 1,
                    Product = product
                }
            }
        };

        _repositoryMock
            .Setup(x => x.GetCartByUserIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _trackingServiceMock
            .Setup(x => x.UpdateStatusAsync(
                It.IsAny<Guid>(),
                TrackingStatus.Processing,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new InvalidOperationException(
                    "Tracking failed."));

        // Act
        var act = () => _service.CheckoutAsync(userId);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Tracking failed.");

        _repositoryMock.Verify(
            x => x.RollbackTransactionAsync(
                _transactionMock.Object,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _repositoryMock.Verify(
            x => x.CommitTransactionAsync(
                It.IsAny<IDbContextTransaction>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetMyOrdersAsync_ShouldReturnUserOrders()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var order1 = new Order.Data.Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Total = 100,
            CreatedAtUtc = DateTime.UtcNow
        };

        var order2 = new Order.Data.Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Total = 200,
            CreatedAtUtc = DateTime.UtcNow
        };

        _repositoryMock
            .Setup(x => x.GetOrdersByUserIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order.Data.Order>
            {
                order1,
                order2
            });

        _repositoryMock
            .Setup(x => x.GetOrderProductsByOrderIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OrderProduct>());

        // Act
        var result = await _service.GetMyOrdersAsync(userId);

        // Assert
        result.Should().HaveCount(2);
        result.Select(x => x.Id)
            .Should()
            .Contain(new[] { order1.Id, order2.Id });
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrowKeyNotFoundException_WhenOrderDoesNotBelongToUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var order = new Order.Data.Order
        {
            Id = orderId,
            UserId = otherUserId,
            Total = 100,
            CreatedAtUtc = DateTime.UtcNow
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(
                orderId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var act = () => _service.GetByIdAsync(
            userId,
            orderId);

        // Assert
        await act.Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage("Order was not found.");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnOrder_WhenOrderBelongsToUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var order = new Order.Data.Order
        {
            Id = orderId,
            UserId = userId,
            Total = 120,
            CreatedAtUtc = DateTime.UtcNow
        };

        var orderProduct = new OrderProduct
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ProductId = productId,
            Quantity = 2,
            UnitPrice = 60
        };

        var product = new Product.Data.Product
        {
            Id = productId,
            Name = "Keyboard",
            Price = 60,
            Stock = 5,
            IsDeleted = false
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(
                orderId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _repositoryMock
            .Setup(x => x.GetOrderProductsByOrderIdAsync(
                orderId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OrderProduct>
            {
                orderProduct
            });

        _repositoryMock
            .Setup(x => x.GetProductByIdAsync(
                productId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _service.GetByIdAsync(
            userId,
            orderId);

        // Assert
        result.Id.Should().Be(orderId);
        result.UserId.Should().Be(userId);
        result.Total.Should().Be(120);

        result.Items.Should().HaveCount(1);
        result.Items[0].ProductId.Should().Be(productId);
        result.Items[0].ProductName.Should().Be("Keyboard");
        result.Items[0].Quantity.Should().Be(2);
        result.Items[0].UnitPrice.Should().Be(60);
        result.Items[0].TotalPrice.Should().Be(120);
    }
}