using Auth.Data;
using Auth.Services;
using AutoMapper;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;

namespace ECommerce.UnitTests.AuthTests;

public class AuthServiceTests
{
    private readonly Mock<IAuthRepository> _repositoryMock;
    private readonly Mock<IPasswordService> _passwordServiceMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock;
    private readonly Mock<IValidator<RegisterRequest>> _registerValidatorMock;
    private readonly Mock<IValidator<LoginRequest>> _loginValidatorMock;
    private readonly Mock<IValidator<RefreshTokenRequest>> _refreshTokenValidatorMock;
    private readonly Mock<IMapper> _mapperMock;

    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _repositoryMock = new Mock<IAuthRepository>();
        _passwordServiceMock = new Mock<IPasswordService>();
        _jwtServiceMock = new Mock<IJwtService>();
        _refreshTokenServiceMock = new Mock<IRefreshTokenService>();
        _registerValidatorMock =
            new Mock<IValidator<RegisterRequest>>();
        _loginValidatorMock =
            new Mock<IValidator<LoginRequest>>();
        _refreshTokenValidatorMock =
            new Mock<IValidator<RefreshTokenRequest>>();
        _mapperMock = new Mock<IMapper>();

        _service = new AuthService(
            _repositoryMock.Object,
            _passwordServiceMock.Object,
            _jwtServiceMock.Object,
            _refreshTokenServiceMock.Object,
            _mapperMock.Object,
            _registerValidatorMock.Object,
            _loginValidatorMock.Object,
            _refreshTokenValidatorMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrowArgumentException_WhenValidationFails()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Name = "Test",
            Email = "invalid",
            Password = "123"
        };

        var validationResult = new ValidationResult(
            new List<ValidationFailure>
            {
                new(
                    nameof(RegisterRequest.Email),
                    "Invalid email.")
            });

        _registerValidatorMock
            .Setup(x => x.ValidateAsync(
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var act = () => _service.RegisterAsync(request);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentException>();

        _repositoryMock.Verify(
            x => x.GetUserByEmailAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrowInvalidOperationException_WhenEmailAlreadyExists()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Name = "Mariam",
            Email = "mariam@test.com",
            Password = "Password123!"
        };

        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Name = "Existing",
            Email = request.Email,
            IsDeleted = false
        };

        _registerValidatorMock
            .Setup(x => x.ValidateAsync(
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _repositoryMock
            .Setup(x => x.GetUserByEmailAsync(
                request.Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act
        var act = () => _service.RegisterAsync(request);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage(
                "A user with this email already exists.");

        _repositoryMock.Verify(
            x => x.AddUser(It.IsAny<User>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrowInvalidOperationException_WhenCustomerRoleDoesNotExist()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Name = "Mariam",
            Email = "mariam@test.com",
            Password = "Password123!"
        };

        _registerValidatorMock
            .Setup(x => x.ValidateAsync(
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _repositoryMock
            .Setup(x => x.GetUserByEmailAsync(
                request.Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _repositoryMock
            .Setup(x => x.GetRoleByNameAsync(
                "Customer",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        // Act
        var act = () => _service.RegisterAsync(request);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Customer role was not found.");

        _repositoryMock.Verify(
            x => x.AddUser(It.IsAny<User>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateCustomerAndTokens_WhenRequestIsValid()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Name = "Mariam",
            Email = "mariam@test.com",
            Password = "Password123!"
        };

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = "Customer",
            Description = "Standard customer account."
        };

        const string passwordHash = "hashed-password";
        const string accessToken = "access-token";
        const string rawRefreshToken = "refresh-token";
        const string refreshTokenHash = "refresh-hash";

        _registerValidatorMock
            .Setup(x => x.ValidateAsync(
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _repositoryMock
            .Setup(x => x.GetUserByEmailAsync(
                request.Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _repositoryMock
            .Setup(x => x.GetRoleByNameAsync(
                "Customer",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        _passwordServiceMock
            .Setup(x => x.HashPassword(request.Password))
            .Returns(passwordHash);

        _jwtServiceMock
            .Setup(x => x.GenerateAccessToken(
                It.IsAny<Guid>(),
                request.Email,
                role.Name))
            .Returns(accessToken);

        _refreshTokenServiceMock
            .Setup(x => x.GenerateToken())
            .Returns(rawRefreshToken);

        _refreshTokenServiceMock
            .Setup(x => x.HashToken(rawRefreshToken))
            .Returns(refreshTokenHash);

        User? createdUser = null;
        RefreshToken? createdRefreshToken = null;

        _repositoryMock
            .Setup(x => x.AddUser(It.IsAny<User>()))
            .Callback<User>(user => createdUser = user);

        _repositoryMock
            .Setup(x => x.AddRefreshToken(It.IsAny<RefreshToken>()))
            .Callback<RefreshToken>(
                token => createdRefreshToken = token);

        // Act
        var result = await _service.RegisterAsync(request);

        // Assert
        result.AccessToken.Should().Be(accessToken);
        result.RefreshToken.Should().Be(rawRefreshToken);
        result.Role.Should().Be("Customer");

        createdUser.Should().NotBeNull();
        createdUser!.Name.Should().Be(request.Name);
        createdUser.Email.Should().Be(request.Email);
        createdUser.PasswordHash.Should().Be(passwordHash);
        createdUser.RoleId.Should().Be(role.Id);
        createdUser.IsDeleted.Should().BeFalse();
        createdUser.CreatedAtUtc.Should().NotBe(default);

        createdRefreshToken.Should().NotBeNull();
        createdRefreshToken!.UserId.Should().Be(createdUser.Id);
        createdRefreshToken.TokenHash.Should().Be(refreshTokenHash);
        createdRefreshToken.CreatedAtUtc.Should().NotBe(default);
        createdRefreshToken.ExpiresAtUtc.Should().BeAfter(
            createdRefreshToken.CreatedAtUtc);

        _repositoryMock.Verify(
            x => x.AddUser(
                It.IsAny<User>()),
            Times.Once);

        _repositoryMock.Verify(
            x => x.AddRefreshToken(
                It.IsAny<RefreshToken>()),
            Times.Once);

        _repositoryMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowArgumentException_WhenValidationFails()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "invalid",
            Password = "123"
        };

        _loginValidatorMock
            .Setup(x => x.ValidateAsync(
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new ValidationResult(
                    new List<ValidationFailure>
                    {
                        new(
                            nameof(LoginRequest.Email),
                            "Invalid email.")
                    }));

        // Act
        var act = () => _service.LoginAsync(request);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentException>();

        _repositoryMock.Verify(
            x => x.GetUserByEmailAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowUnauthorizedAccessException_WhenUserDoesNotExist()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "missing@test.com",
            Password = "Password123!"
        };

        _loginValidatorMock
            .Setup(x => x.ValidateAsync(
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _repositoryMock
            .Setup(x => x.GetUserByEmailAsync(
                request.Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var act = () => _service.LoginAsync(request);

        // Assert
        await act.Should()
            .ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowUnauthorizedAccessException_WhenPasswordIsInvalid()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "user@test.com",
            Password = "WrongPassword!"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = request.Email,
            PasswordHash = "hashed-password",
            RoleId = Guid.NewGuid(),
            IsDeleted = false
        };

        _loginValidatorMock
            .Setup(x => x.ValidateAsync(
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _repositoryMock
            .Setup(x => x.GetUserByEmailAsync(
                request.Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordServiceMock
            .Setup(x => x.VerifyPassword(
                request.Password,
                user.PasswordHash))
            .Returns(false);

        // Act
        var act = () => _service.LoginAsync(request);

        // Assert
        await act.Should()
            .ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid email or password.");

        _repositoryMock.Verify(
            x => x.GetRoleByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowInvalidOperationException_WhenRoleDoesNotExist()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "user@test.com",
            Password = "Password123!"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = request.Email,
            PasswordHash = "hashed-password",
            RoleId = Guid.NewGuid(),
            IsDeleted = false
        };

        _loginValidatorMock
            .Setup(x => x.ValidateAsync(
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _repositoryMock
            .Setup(x => x.GetUserByEmailAsync(
                request.Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordServiceMock
            .Setup(x => x.VerifyPassword(
                request.Password,
                user.PasswordHash))
            .Returns(true);

        _repositoryMock
            .Setup(x => x.GetRoleByIdAsync(
                user.RoleId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        // Act
        var act = () => _service.LoginAsync(request);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("User role was not found.");
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnTokens_WhenCredentialsAreValid()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "user@test.com",
            Password = "Password123!"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = request.Email,
            PasswordHash = "hashed-password",
            RoleId = Guid.NewGuid(),
            IsDeleted = false
        };

        var role = new Role
        {
            Id = user.RoleId,
            Name = "Customer",
            Description = "Standard customer account."
        };

        const string accessToken = "access-token";
        const string rawRefreshToken = "refresh-token";
        const string refreshTokenHash = "refresh-hash";

        _loginValidatorMock
            .Setup(x => x.ValidateAsync(
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _repositoryMock
            .Setup(x => x.GetUserByEmailAsync(
                request.Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordServiceMock
            .Setup(x => x.VerifyPassword(
                request.Password,
                user.PasswordHash))
            .Returns(true);

        _repositoryMock
            .Setup(x => x.GetRoleByIdAsync(
                user.RoleId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        _jwtServiceMock
            .Setup(x => x.GenerateAccessToken(
                user.Id,
                user.Email,
                role.Name))
            .Returns(accessToken);

        _refreshTokenServiceMock
            .Setup(x => x.GenerateToken())
            .Returns(rawRefreshToken);

        _refreshTokenServiceMock
            .Setup(x => x.HashToken(rawRefreshToken))
            .Returns(refreshTokenHash);

        // Act
        var result = await _service.LoginAsync(request);

        // Assert
        result.UserId.Should().Be(user.Id);
        result.Role.Should().Be(role.Name);
        result.AccessToken.Should().Be(accessToken);
        result.RefreshToken.Should().Be(rawRefreshToken);

        _repositoryMock.Verify(
            x => x.AddRefreshToken(
                It.Is<RefreshToken>(token =>
                    token.UserId == user.Id &&
                    token.TokenHash == refreshTokenHash)),
            Times.Once);

        _repositoryMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldThrowArgumentException_WhenValidationFails()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = ""
        };

        _refreshTokenValidatorMock
            .Setup(x => x.ValidateAsync(
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new ValidationResult(
                    new List<ValidationFailure>
                    {
                        new(
                            nameof(RefreshTokenRequest.RefreshToken),
                            "Refresh token is required.")
                    }));

        // Act
        var act = () => _service.RefreshTokenAsync(request);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentException>();

        _repositoryMock.Verify(
            x => x.GetRefreshTokenAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldThrowUnauthorizedAccessException_WhenTokenDoesNotExist()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = "refresh-token"
        };

        _refreshTokenValidatorMock
            .Setup(x => x.ValidateAsync(
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _refreshTokenServiceMock
            .Setup(x => x.HashToken(request.RefreshToken))
            .Returns("hash");

        _repositoryMock
            .Setup(x => x.GetRefreshTokenAsync(
                "hash",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        var act = () => _service.RefreshTokenAsync(request);

        // Assert
        await act.Should()
            .ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid refresh token.");
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldThrowUnauthorizedAccessException_WhenTokenIsRevoked()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = "refresh-token"
        };

        var storedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TokenHash = "hash",
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
            RevokedAtUtc = DateTime.UtcNow
        };

        _refreshTokenValidatorMock
            .Setup(x => x.ValidateAsync(
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _refreshTokenServiceMock
            .Setup(x => x.HashToken(request.RefreshToken))
            .Returns("hash");

        _repositoryMock
            .Setup(x => x.GetRefreshTokenAsync(
                "hash",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);

        // Act
        var act = () => _service.RefreshTokenAsync(request);

        // Assert
        await act.Should()
            .ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid refresh token.");
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldThrowUnauthorizedAccessException_WhenTokenIsExpired()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = "refresh-token"
        };

        var storedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TokenHash = "hash",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-8),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1)
        };

        _refreshTokenValidatorMock
            .Setup(x => x.ValidateAsync(
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _refreshTokenServiceMock
            .Setup(x => x.HashToken(request.RefreshToken))
            .Returns("hash");

        _repositoryMock
            .Setup(x => x.GetRefreshTokenAsync(
                "hash",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);

        // Act
        var act = () => _service.RefreshTokenAsync(request);

        // Assert
        await act.Should()
            .ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Refresh token has expired.");
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldRevokeOldTokenAndCreateNewTokens_WhenTokenIsValid()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = "old-refresh-token"
        };

        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var storedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = "old-hash",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(6)
        };

        var user = new User
        {
            Id = userId,
            Name = "Test User",
            Email = "user@test.com",
            PasswordHash = "hash",
            RoleId = roleId,
            IsDeleted = false
        };

        var role = new Role
        {
            Id = roleId,
            Name = "Customer",
            Description = "Standard customer account."
        };

        const string newAccessToken = "new-access-token";
        const string newRawRefreshToken = "new-refresh-token";
        const string newRefreshTokenHash = "new-hash";

        _refreshTokenValidatorMock
            .Setup(x => x.ValidateAsync(
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _refreshTokenServiceMock
            .Setup(x => x.HashToken(request.RefreshToken))
            .Returns("old-hash");

        _repositoryMock
            .Setup(x => x.GetRefreshTokenAsync(
                "old-hash",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);

        _repositoryMock
            .Setup(x => x.GetUserByIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _repositoryMock
            .Setup(x => x.GetRoleByIdAsync(
                roleId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        _jwtServiceMock
            .Setup(x => x.GenerateAccessToken(
                user.Id,
                user.Email,
                role.Name))
            .Returns(newAccessToken);

        _refreshTokenServiceMock
            .Setup(x => x.GenerateToken())
            .Returns(newRawRefreshToken);

        _refreshTokenServiceMock
            .Setup(x => x.HashToken(newRawRefreshToken))
            .Returns(newRefreshTokenHash);

        // Act
        var result = await _service.RefreshTokenAsync(request);

        // Assert
        result.AccessToken.Should().Be(newAccessToken);
        result.RefreshToken.Should().Be(newRawRefreshToken);
        result.UserId.Should().Be(userId);
        result.Role.Should().Be("Customer");

        storedToken.RevokedAtUtc.Should().NotBeNull();

        _repositoryMock.Verify(
            x => x.AddRefreshToken(
                It.Is<RefreshToken>(token =>
                    token.UserId == userId &&
                    token.TokenHash == newRefreshTokenHash)),
            Times.Once);

        _repositoryMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_ShouldDoNothing_WhenTokenDoesNotExist()
    {
        // Arrange
        const string rawToken = "missing-token";

        _refreshTokenServiceMock
            .Setup(x => x.HashToken(rawToken))
            .Returns("hash");

        _repositoryMock
            .Setup(x => x.GetRefreshTokenAsync(
                "hash",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        await _service.LogoutAsync(rawToken);

        // Assert
        _repositoryMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task LogoutAsync_ShouldRevokeToken_WhenTokenIsActive()
    {
        // Arrange
        const string rawToken = "refresh-token";

        var storedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TokenHash = "hash",
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1)
        };

        _refreshTokenServiceMock
            .Setup(x => x.HashToken(rawToken))
            .Returns("hash");

        _repositoryMock
            .Setup(x => x.GetRefreshTokenAsync(
                "hash",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);

        // Act
        await _service.LogoutAsync(rawToken);

        // Assert
        storedToken.RevokedAtUtc.Should().NotBeNull();

        _repositoryMock.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetProfileAsync_ShouldThrowUnauthorizedAccessException_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.GetUserByIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var act = () => _service.GetProfileAsync(userId);

        // Assert
        await act.Should()
            .ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("User account was not found.");
    }

    [Fact]
    public async Task GetProfileAsync_ShouldThrowInvalidOperationException_WhenRoleDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Name = "Test User",
            Email = "user@test.com",
            RoleId = roleId,
            IsDeleted = false
        };

        _repositoryMock
            .Setup(x => x.GetUserByIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _repositoryMock
            .Setup(x => x.GetRoleByIdAsync(
                roleId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        // Act
        var act = () => _service.GetProfileAsync(userId);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("User role was not found.");
    }

    [Fact]
    public async Task GetProfileAsync_ShouldReturnProfile_WhenUserAndRoleExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Name = "Mariam",
            Email = "mariam@test.com",
            RoleId = roleId,
            IsDeleted = false
        };

        var role = new Role
        {
            Id = roleId,
            Name = "Customer",
            Description = "Standard customer account."
        };

        var mappedResponse = new ProfileResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email
        };

        _repositoryMock
            .Setup(x => x.GetUserByIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _repositoryMock
            .Setup(x => x.GetRoleByIdAsync(
                roleId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        _mapperMock
            .Setup(x => x.Map<ProfileResponse>(user))
            .Returns(mappedResponse);

        // Act
        var result = await _service.GetProfileAsync(userId);

        // Assert
        result.Should().BeSameAs(mappedResponse);
        result.Id.Should().Be(userId);
        result.Name.Should().Be("Mariam");
        result.Email.Should().Be("mariam@test.com");
        result.Role.Should().Be("Customer");
    }
}