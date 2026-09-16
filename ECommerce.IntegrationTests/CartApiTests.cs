using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;

namespace ECommerce.IntegrationTests;

public class CartApiTests :
    IClassFixture<CartWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CartApiTests(
        CartWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCart_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Act
        var response = await _client.GetAsync(
            "/api/cart");

        // Assert
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCart_ShouldReturnOk_WhenUserIsAuthenticated()
    {
        // Arrange
        var userId = CartWebApplicationFactory.TestUserId;

        SetCustomerToken(userId);

        // Act
        var response = await _client.GetAsync(
            "/api/cart");

        // Assert
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AddProduct_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var request = new
        {
            productId = Guid.NewGuid(),
            quantity = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/cart/products",
            request);

        // Assert
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Unauthorized);
    }

   

    [Fact]
    public async Task RemoveProduct_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync(
            $"/api/cart/products/{productId}");

        // Assert
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ClearCart_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Act
        var response = await _client.DeleteAsync(
            "/api/cart");

        // Assert
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    private void SetCustomerToken(Guid userId)
    {
        var token = TestJwtHelper.CreateToken(
            userId,
            $"cart{userId}@example.com",
            "Customer");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);
    }
}