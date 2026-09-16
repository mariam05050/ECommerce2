using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;

namespace ECommerce.IntegrationTests;

public class AuthApiTests :
    IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthApiTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ShouldReturnOk()
    {
        // Arrange
        var request = new
        {
            name = "Integration Test User",
            email = $"test{Guid.NewGuid()}@example.com",
            password = "Password123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        // Assert
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_ShouldReturnOk_WhenCredentialsAreValid()
    {
        // Arrange
        var email = $"login{Guid.NewGuid()}@example.com";
        const string password = "Password123!";

        await RegisterUserAsync(email, password);

        var request = new
        {
            email,
            password
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            request);

        // Assert
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenPasswordIsWrong()
    {
        // Arrange
        var email = $"wrong{Guid.NewGuid()}@example.com";

        await RegisterUserAsync(
            email,
            "Password123!");

        var request = new
        {
            email,
            password = "WrongPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            request);

        // Assert
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Profile_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Act
        var response = await _client.GetAsync(
            "/api/auth/profile");

        // Assert
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Profile_ShouldReturnOk_WhenAuthenticated()
    {
        // Arrange
        var email = $"profile{Guid.NewGuid()}@example.com";
        const string password = "Password123!";

        await RegisterUserAsync(email, password);

        var loginResponse = await LoginAsync(
            email,
            password);

        loginResponse.AccessToken
            .Should()
            .NotBeNullOrWhiteSpace();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                loginResponse.AccessToken);

        // Act
        var response = await _client.GetAsync(
            "/api/auth/profile");

        // Assert
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var profile =
            await response.Content.ReadFromJsonAsync<ProfileResponse>();

        profile.Should().NotBeNull();
        profile!.Email.Should().Be(email);
        profile.Name.Should().Be("Integration Test User");
        profile.Role.Should().Be("Customer");
    }

    [Fact]
    public async Task Refresh_ShouldReturnOk_WhenRefreshTokenIsValid()
    {
        // Arrange
        var email = $"refresh{Guid.NewGuid()}@example.com";
        const string password = "Password123!";

        await RegisterUserAsync(email, password);

        var loginResponse = await LoginAsync(
            email,
            password);

        var request = new
        {
            refreshToken = loginResponse.RefreshToken
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/auth/refresh",
            request);

        // Assert
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK);

        var refreshed =
            await response.Content.ReadFromJsonAsync<AuthResponse>();

        refreshed.Should().NotBeNull();
        refreshed!.AccessToken.Should().NotBeNullOrWhiteSpace();
        refreshed.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Logout_ShouldReturnNoContent_WhenAuthenticated()
    {
        // Arrange
        var email = $"logout{Guid.NewGuid()}@example.com";
        const string password = "Password123!";

        await RegisterUserAsync(email, password);

        var loginResponse = await LoginAsync(
            email,
            password);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                loginResponse.AccessToken);

        var request = new
        {
            refreshToken = loginResponse.RefreshToken
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/auth/logout",
            request);

        // Assert
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.NoContent);
    }

    private async Task RegisterUserAsync(
        string email,
        string password)
    {
        var request = new
        {
            name = "Integration Test User",
            email,
            password
        };

        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            request);

        response.EnsureSuccessStatusCode();
    }

    private async Task<AuthResponse> LoginAsync(
        string email,
        string password)
    {
        var request = new
        {
            email,
            password
        };

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            request);

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content.ReadFromJsonAsync<AuthResponse>();

        result.Should().NotBeNull();

        return result!;
    }

    private class AuthResponse
    {
        public string AccessToken { get; set; } = string.Empty;

        public string RefreshToken { get; set; } = string.Empty;

        public Guid UserId { get; set; }

        public string Role { get; set; } = string.Empty;
    }

    private class ProfileResponse
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;
    }
}