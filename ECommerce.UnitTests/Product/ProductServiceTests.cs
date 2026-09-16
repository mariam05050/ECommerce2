using AutoMapper;
using FluentAssertions;
using Moq;
using Product.Data;
using Product.Services;

namespace ECommerce.UnitTests.ProductTests;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _repositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly ProductService _service;

    public ProductServiceTests()
    {
        _repositoryMock = new Mock<IProductRepository>();
        _mapperMock = new Mock<IMapper>();

        _service = new ProductService(
            _repositoryMock.Object,
            _mapperMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnMappedProducts()
    {
        // Arrange
        var products = new List<Product.Data.Product>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Keyboard",
                Price = 60,
                Stock = 10,
                IsDeleted = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Mouse",
                Price = 30,
                Stock = 5,
                IsDeleted = false
            }
        };

        var responses = new List<ProductResponse>
        {
            new(),
            new()
        };

        _repositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _mapperMock
            .Setup(x => x.Map<List<ProductResponse>>(products))
            .Returns(responses);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().BeEquivalentTo(responses);

        _repositoryMock.Verify(
            x => x.GetAllAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnProduct_WhenProductExists()
    {
        // Arrange
        var id = Guid.NewGuid();

        var product = new Product.Data.Product
        {
            Id = id,
            Name = "Keyboard",
            Price = 60,
            Stock = 10,
            IsDeleted = false
        };

        var response = new ProductResponse();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _mapperMock
            .Setup(x => x.Map<ProductResponse>(product))
            .Returns(response);

        // Act
        var result = await _service.GetByIdAsync(id);

        // Assert
        result.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrowKeyNotFoundException_WhenProductDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product.Data.Product?)null);

        // Act
        var act = () => _service.GetByIdAsync(id);

        // Assert
        await act.Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Product with ID '{id}' was not found.");
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowConflict_WhenProductNameAlreadyExists()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Name = "Keyboard",
            Description = "Mechanical keyboard",
            Price = 60,
            Stock = 10
        };

        _repositoryMock
            .Setup(x => x.ExistsByNameAsync(
                request.Name,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => _service.CreateAsync(request);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage(
                "A product with the same name already exists.");

        _repositoryMock.Verify(
            x => x.Add(It.IsAny<Product.Data.Product>()),
            Times.Never);

        _repositoryMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateProduct_WhenNameDoesNotExist()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Name = "Keyboard",
            Description = "Mechanical keyboard",
            Price = 60,
            Stock = 10
        };

        var product = new Product.Data.Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Stock = request.Stock
        };

        var response = new ProductResponse();

        _repositoryMock
            .Setup(x => x.ExistsByNameAsync(
                request.Name,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mapperMock
            .Setup(x => x.Map<Product.Data.Product>(request))
            .Returns(product);

        _mapperMock
            .Setup(x => x.Map<ProductResponse>(product))
            .Returns(response);

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        result.Should().BeEquivalentTo(response);

        product.Id.Should().NotBeEmpty();
        product.CreatedAtUtc.Should().NotBe(default);
        product.IsDeleted.Should().BeFalse();

        _repositoryMock.Verify(
            x => x.Add(product),
            Times.Once);

        _repositoryMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowKeyNotFoundException_WhenProductDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();

        var request = new UpdateProductRequest
        {
            Name = "Keyboard",
            Description = "Mechanical keyboard",
            Price = 60,
            Stock = 10
        };

        _repositoryMock
            .Setup(x => x.GetByIdForUpdateAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product.Data.Product?)null);

        // Act
        var act = () => _service.UpdateAsync(id, request);

        // Assert
        await act.Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Product with ID '{id}' was not found.");
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowConflict_WhenNameAlreadyExists()
    {
        // Arrange
        var id = Guid.NewGuid();

        var request = new UpdateProductRequest
        {
            Name = "Keyboard",
            Description = "Mechanical keyboard",
            Price = 60,
            Stock = 10
        };

        var product = new Product.Data.Product
        {
            Id = id,
            Name = "Old Keyboard",
            Description = "Old description",
            Price = 50,
            Stock = 5,
            IsDeleted = false
        };

        _repositoryMock
            .Setup(x => x.GetByIdForUpdateAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _repositoryMock
            .Setup(x => x.ExistsByNameAsync(
                request.Name,
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => _service.UpdateAsync(id, request);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage(
                "A product with the same name already exists.");

        _repositoryMock.Verify(
            x => x.Update(It.IsAny<Product.Data.Product>()),
            Times.Never);

        _repositoryMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateProduct_WhenValid()
    {
        // Arrange
        var id = Guid.NewGuid();

        var request = new UpdateProductRequest
        {
            Name = "Updated Keyboard",
            Description = "Updated description",
            Price = 75,
            Stock = 20
        };

        var product = new Product.Data.Product
        {
            Id = id,
            Name = "Keyboard",
            Description = "Old description",
            Price = 60,
            Stock = 10,
            IsDeleted = false
        };

        var response = new ProductResponse();

        _repositoryMock
            .Setup(x => x.GetByIdForUpdateAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _repositoryMock
            .Setup(x => x.ExistsByNameAsync(
                request.Name,
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mapperMock
            .Setup(x => x.Map<ProductResponse>(product))
            .Returns(response);

        // Act
        var result = await _service.UpdateAsync(id, request);

        // Assert
        result.Should().BeEquivalentTo(response);

        product.Name.Should().Be(request.Name);
        product.Description.Should().Be(request.Description);
        product.Price.Should().Be(request.Price);
        product.Stock.Should().Be(request.Stock);
        product.UpdatedAtUtc.Should().NotBe(default);

        _repositoryMock.Verify(
            x => x.Update(product),
            Times.Once);

        _repositoryMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowKeyNotFoundException_WhenProductDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.GetByIdForUpdateAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product.Data.Product?)null);

        // Act
        var act = () => _service.DeleteAsync(id);

        // Assert
        await act.Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Product with ID '{id}' was not found.");
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteProduct_WhenProductExists()
    {
        // Arrange
        var id = Guid.NewGuid();

        var product = new Product.Data.Product
        {
            Id = id,
            Name = "Keyboard",
            Description = "Mechanical keyboard",
            Price = 60,
            Stock = 10,
            IsDeleted = false
        };

        _repositoryMock
            .Setup(x => x.GetByIdForUpdateAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        await _service.DeleteAsync(id);

        // Assert
        product.IsDeleted.Should().BeTrue();
        product.UpdatedAtUtc.Should().NotBe(default);

        _repositoryMock.Verify(
            x => x.Update(product),
            Times.Once);

        _repositoryMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}