using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;

namespace ECommerce.IntegrationTests;

public class TrackingApiTests :
    IClassFixture<TrackingWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TrackingApiTests(
        TrackingWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetTrackingUnauthorized()
    {
        // Act
        var response = await _client.GetAsync(
            $"/api/tracking/orders/{TrackingWebApplicationFactory.TestOrderId}");

        // Assert
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTracking()
    {
        // Arrange
        SetCustomerToken();

        // Act
        var response = await _client.GetAsync(
            $"/api/tracking/orders/{TrackingWebApplicationFactory.TestOrderId}");

        // Assert
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);
    }


    [Fact]
    public async Task UpdateStatus()
    {
        // Arrange
        SetAdminToken();

        // Act
        var response = await _client.PostAsync(
            $"/api/tracking/orders/{TrackingWebApplicationFactory.TestOrderId}/status/Shipped",
            null);

        // Assert
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateStatusInvalid()
    {
        // Arrange
        SetAdminToken();

        // Act
        var response = await _client.PostAsync(
            $"/api/tracking/orders/{TrackingWebApplicationFactory.TestOrderId}/status/InvalidStatus",
            null);

        // Assert
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.BadRequest);
    }

    private void SetAdminToken()
    {
        var token = TestJwtHelper.CreateToken(
            TrackingWebApplicationFactory.TestUserId,
            "tracking-admin@test.com",
            "Admin");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);
    }

    [Fact]
    public async Task UpdateStatusUnauthorized()
    {
        // Act
        var response = await _client.PostAsync(
            $"/api/tracking/orders/{TrackingWebApplicationFactory.TestOrderId}/status/Shipped",
            null);

        // Assert
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    private void SetCustomerToken()
    {
        var token = TestJwtHelper.CreateToken(
            TrackingWebApplicationFactory.TestUserId,
            "tracking-integration@test.com",
            "Customer");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);
    }
}