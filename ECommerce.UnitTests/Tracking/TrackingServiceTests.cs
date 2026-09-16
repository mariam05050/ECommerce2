using FluentAssertions;
using Moq;
using Tracking.Data;
using Tracking.Services;

namespace ECommerce.UnitTests.TrackingTests;

public class TrackingServiceTests
{
    private readonly Mock<ITrackingRepository> _repositoryMock;
    private readonly TrackingService _service;

    public TrackingServiceTests()
    {
        _repositoryMock = new Mock<ITrackingRepository>();

        _service = new TrackingService(
            _repositoryMock.Object);
    }

    [Fact]
    public async Task GetTrackingAsync_ShouldThrowKeyNotFoundException_WhenOrderDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.GetOrderByIdAsync(
                orderId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order.Data.Order?)null);

        // Act
        var act = () => _service.GetTrackingAsync(
            userId,
            orderId);

        // Assert
        await act.Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage("Order was not found.");

        _repositoryMock.Verify(
            x => x.GetByOrderIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetTrackingAsync_ShouldThrowKeyNotFoundException_WhenOrderBelongsToAnotherUser()
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
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };

        _repositoryMock
            .Setup(x => x.GetOrderByIdAsync(
                orderId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var act = () => _service.GetTrackingAsync(
            userId,
            orderId);

        // Assert
        await act.Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage("Order was not found.");

        _repositoryMock.Verify(
            x => x.GetByOrderIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetTrackingAsync_ShouldReturnTrackingHistory_WhenOrderBelongsToUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var order = new Order.Data.Order
        {
            Id = orderId,
            UserId = userId,
            Total = 200,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };

        var events = new List<TrackingEvent>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                Status = TrackingStatus.Processing,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-30)
            },
            new()
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                Status = TrackingStatus.Shipped,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-15)
            },
            new()
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                Status = TrackingStatus.OutForDelivery,
                CreatedAtUtc = DateTime.UtcNow
            }
        };

        _repositoryMock
            .Setup(x => x.GetOrderByIdAsync(
                orderId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _repositoryMock
            .Setup(x => x.GetByOrderIdAsync(
                orderId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(events);

        // Act
        var result = await _service.GetTrackingAsync(
            userId,
            orderId);

        // Assert
        result.OrderId.Should().Be(orderId);
        result.Events.Should().HaveCount(3);

        result.Events[0].Status
            .Should().Be(TrackingStatus.Processing);

        result.Events[1].Status
            .Should().Be(TrackingStatus.Shipped);

        result.Events[2].Status
            .Should().Be(TrackingStatus.OutForDelivery);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldThrowKeyNotFoundException_WhenOrderDoesNotExist()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.GetOrderByIdAsync(
                orderId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order.Data.Order?)null);

        // Act
        var act = () => _service.UpdateStatusAsync(
            orderId,
            TrackingStatus.Processing);

        // Assert
        await act.Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage("Order was not found.");
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldThrowKeyNotFoundException_WhenOrderIsDeleted()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        var order = new Order.Data.Order
        {
            Id = orderId,
            UserId = Guid.NewGuid(),
            Total = 100,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = true
        };

        _repositoryMock
            .Setup(x => x.GetOrderByIdAsync(
                orderId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var act = () => _service.UpdateStatusAsync(
            orderId,
            TrackingStatus.Processing);

        // Assert
        await act.Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage("Order was not found.");
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldAllowProcessing_WhenOrderHasNoPreviousEvent()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        var order = new Order.Data.Order
        {
            Id = orderId,
            UserId = Guid.NewGuid(),
            Total = 100,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };

        _repositoryMock
            .Setup(x => x.GetOrderByIdAsync(
                orderId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _repositoryMock
            .Setup(x => x.GetLatestByOrderIdAsync(
                orderId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((TrackingEvent?)null);

        // Act
        var result = await _service.UpdateStatusAsync(
            orderId,
            TrackingStatus.Processing);

        // Assert
        result.Status.Should().Be(TrackingStatus.Processing);

        _repositoryMock.Verify(
            x => x.AddTrackingEvent(
                It.Is<TrackingEvent>(e =>
                    e.OrderId == orderId &&
                    e.Status == TrackingStatus.Processing)),
            Times.Once);

        _repositoryMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldRejectNonProcessingStatus_WhenOrderHasNoPreviousEvent()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        var order = new Order.Data.Order
        {
            Id = orderId,
            UserId = Guid.NewGuid(),
            Total = 100,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };

        _repositoryMock
            .Setup(x => x.GetOrderByIdAsync(
                orderId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _repositoryMock
            .Setup(x => x.GetLatestByOrderIdAsync(
                orderId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((TrackingEvent?)null);

        // Act
        var act = () => _service.UpdateStatusAsync(
            orderId,
            TrackingStatus.Delivered);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage(
                "A new order must start with Processing status.");

        _repositoryMock.Verify(
            x => x.AddTrackingEvent(
                It.IsAny<TrackingEvent>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldRejectSameStatus()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        var order = new Order.Data.Order
        {
            Id = orderId,
            UserId = Guid.NewGuid(),
            Total = 100,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };

        var latestEvent = new TrackingEvent
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Status = TrackingStatus.Processing,
            CreatedAtUtc = DateTime.UtcNow
        };

        _repositoryMock
            .Setup(x => x.GetOrderByIdAsync(
                orderId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _repositoryMock
            .Setup(x => x.GetLatestByOrderIdAsync(
                orderId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(latestEvent);

        // Act
        var act = () => _service.UpdateStatusAsync(
            orderId,
            TrackingStatus.Processing);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage(
                "The order already has this status.");

        _repositoryMock.Verify(
            x => x.AddTrackingEvent(
                It.IsAny<TrackingEvent>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldAllowValidTransition()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        var order = new Order.Data.Order
        {
            Id = orderId,
            UserId = Guid.NewGuid(),
            Total = 100,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };

        var latestEvent = new TrackingEvent
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Status = TrackingStatus.Processing,
            CreatedAtUtc = DateTime.UtcNow
        };

        _repositoryMock
            .Setup(x => x.GetOrderByIdAsync(
                orderId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _repositoryMock
            .Setup(x => x.GetLatestByOrderIdAsync(
                orderId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(latestEvent);

        // Act
        var result = await _service.UpdateStatusAsync(
            orderId,
            TrackingStatus.Shipped);

        // Assert
        result.Status.Should().Be(TrackingStatus.Shipped);

        _repositoryMock.Verify(
            x => x.AddTrackingEvent(
                It.Is<TrackingEvent>(e =>
                    e.OrderId == orderId &&
                    e.Status == TrackingStatus.Shipped)),
            Times.Once);

        _repositoryMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldRejectInvalidTransition()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        var order = new Order.Data.Order
        {
            Id = orderId,
            UserId = Guid.NewGuid(),
            Total = 100,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };

        var latestEvent = new TrackingEvent
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Status = TrackingStatus.Processing,
            CreatedAtUtc = DateTime.UtcNow
        };

        _repositoryMock
            .Setup(x => x.GetOrderByIdAsync(
                orderId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _repositoryMock
            .Setup(x => x.GetLatestByOrderIdAsync(
                orderId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(latestEvent);

        // Act
        var act = () => _service.UpdateStatusAsync(
            orderId,
            TrackingStatus.Delivered);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage(
                "Cannot change order status from Processing to Delivered.");

        _repositoryMock.Verify(
            x => x.AddTrackingEvent(
                It.IsAny<TrackingEvent>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldRejectStatusAfterDelivered()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        var order = new Order.Data.Order
        {
            Id = orderId,
            UserId = Guid.NewGuid(),
            Total = 100,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };

        var latestEvent = new TrackingEvent
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Status = TrackingStatus.Delivered,
            CreatedAtUtc = DateTime.UtcNow
        };

        _repositoryMock
            .Setup(x => x.GetOrderByIdAsync(
                orderId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _repositoryMock
            .Setup(x => x.GetLatestByOrderIdAsync(
                orderId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(latestEvent);

        // Act
        var act = () => _service.UpdateStatusAsync(
            orderId,
            TrackingStatus.Shipped);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage(
                "Cannot change order status from Delivered to Shipped.");

        _repositoryMock.Verify(
            x => x.AddTrackingEvent(
                It.IsAny<TrackingEvent>()),
            Times.Never);
    }
}