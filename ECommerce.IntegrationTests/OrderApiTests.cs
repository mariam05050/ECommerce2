using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;

namespace ECommerce.IntegrationTests;

public class OrderApiTests :
    IClassFixture<OrderWebApplicationFactory>
{
    private readonly HttpClient _client;

    public OrderApiTests(
        OrderWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetOrders()
    {
        // Arrange
        SetCustomerToken();

        // Act
        var response = await _client.GetAsync(
            "/api/orders");

        // Assert
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOrdersUnauthorized()
    {
        // Act
        var response = await _client.GetAsync(
            "/api/orders");

        // Assert
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOrderUnauthorized()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync(
            $"/api/orders/{orderId}");

        // Assert
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Checkout()
    {
        // Arrange
        SetCustomerToken();

        // Act
        var response = await _client.PostAsync(
            "/api/orders/checkout",
            null);

        // Assert
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Created);

        var order =
            await response.Content
                .ReadFromJsonAsync<OrderResponse>();

        order.Should().NotBeNull();

        order!.UserId.Should()
            .Be(OrderWebApplicationFactory.TestUserId);

        order.Total.Should().Be(100);

        order.Items.Should().HaveCount(1);

        order.Items[0].ProductId.Should()
            .Be(OrderWebApplicationFactory.TestProductId);

        order.Items[0].Quantity.Should().Be(2);

        order.Items[0].UnitPrice.Should().Be(50);

        order.Items[0].TotalPrice.Should().Be(100);
    }

    private void SetCustomerToken()
    {
        var token = TestJwtHelper.CreateToken(
            OrderWebApplicationFactory.TestUserId,
            "order-integration@test.com",
            "Customer");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);
    }

    private class OrderResponse
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public decimal Total { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public List<OrderItemResponse> Items { get; set; }
            = new();
    }

    private class OrderItemResponse
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; } =
            string.Empty;

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }
    }
}