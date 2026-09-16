using System.Net;
using FluentAssertions;

namespace ECommerce.IntegrationTests;

public class ProductApiTests :
    IClassFixture<ProductWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProductApiTests(
        ProductWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProducts_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync(
            "/api/products");

        // Assert
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);
    }
}