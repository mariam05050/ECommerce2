using Auth.Data;
using AutoMapper;
using FluentValidation;

namespace Auth.Services;

public class AuthService : IAuthService
{
    private readonly IAuthRepository _repository;
    private readonly IPasswordService _passwordService;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RefreshTokenRequest> _refreshTokenValidator;
    private readonly IMapper _mapper;
    public AuthService(
     IAuthRepository repository,
     IPasswordService passwordService,
     IJwtService jwtService,
     IRefreshTokenService refreshTokenService,
     IMapper mapper,
     IValidator<RegisterRequest> registerValidator,
     IValidator<LoginRequest> loginValidator,
     IValidator<RefreshTokenRequest> refreshTokenValidator)
    {
        _repository = repository;
        _passwordService = passwordService;
        _jwtService = jwtService;
        _refreshTokenService = refreshTokenService;
        _mapper = mapper;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _refreshTokenValidator = refreshTokenValidator;
    }

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _registerValidator.ValidateAsync(
            request,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            throw new ArgumentException(
                validationResult.ToString());
        }

        var existingUser = await _repository.GetUserByEmailAsync(
            request.Email,
            cancellationToken);

        if (existingUser is not null)
        {
            throw new InvalidOperationException(
                "A user with this email already exists.");
        }

        var customerRole = await _repository.GetRoleByNameAsync(
            "Customer",
            cancellationToken);

        if (customerRole is null)
        {
            throw new InvalidOperationException(
                "Customer role was not found.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            PasswordHash = _passwordService.HashPassword(
                request.Password),
            RoleId = customerRole.Id,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };

        _repository.AddUser(user);

        await _repository.SaveChangesAsync(cancellationToken);

        var accessToken = _jwtService.GenerateAccessToken(
            user.Id,
            user.Email,
            customerRole.Name);

        var rawRefreshToken = _refreshTokenService.GenerateToken();

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = _refreshTokenService.HashToken(
                rawRefreshToken),
            UserId = user.Id,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        };

        _repository.AddRefreshToken(refreshToken);

        await _repository.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            UserId = user.Id,
            Role = customerRole.Name
        };
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _loginValidator.ValidateAsync(
            request,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            throw new ArgumentException(
                validationResult.ToString());
        }

        var user = await _repository.GetUserByEmailAsync(
            request.Email,
            cancellationToken);

        if (user is null || user.IsDeleted)
        {
            throw new UnauthorizedAccessException(
                "Invalid email or password.");
        }

        var passwordValid = _passwordService.VerifyPassword(
            request.Password,
            user.PasswordHash);

        if (!passwordValid)
        {
            throw new UnauthorizedAccessException(
                "Invalid email or password.");
        }

        var role = await _repository.GetRoleByIdAsync(
            user.RoleId,
            cancellationToken);

        if (role is null)
        {
            throw new InvalidOperationException(
                "User role was not found.");
        }

        var accessToken = _jwtService.GenerateAccessToken(
            user.Id,
            user.Email,
            role.Name);

        var rawRefreshToken = _refreshTokenService.GenerateToken();

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = _refreshTokenService.HashToken(
                rawRefreshToken),
            UserId = user.Id,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        };

        _repository.AddRefreshToken(refreshToken);

        await _repository.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            UserId = user.Id,
            Role = role.Name
        };
    }

    public async Task<AuthResponse> RefreshTokenAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _refreshTokenValidator.ValidateAsync(
            request,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            throw new ArgumentException(
                validationResult.ToString());
        }

        var tokenHash = _refreshTokenService.HashToken(
            request.RefreshToken);

        var storedToken = await _repository.GetRefreshTokenAsync(
            tokenHash,
            cancellationToken);

        if (storedToken is null)
        {
            throw new UnauthorizedAccessException(
                "Invalid refresh token.");
        }

        if (storedToken.RevokedAtUtc is not null)
        {
            throw new UnauthorizedAccessException(
                "Invalid refresh token.");
        }

        if (storedToken.ExpiresAtUtc <= DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException(
                "Refresh token has expired.");
        }

        var user = await _repository.GetUserByIdAsync(
            storedToken.UserId,
            cancellationToken);

        if (user is null || user.IsDeleted)
        {
            throw new UnauthorizedAccessException(
                "Invalid refresh token.");
        }

        var role = await _repository.GetRoleByIdAsync(
            user.RoleId,
            cancellationToken);

        if (role is null)
        {
            throw new InvalidOperationException(
                "User role was not found.");
        }

        storedToken.RevokedAtUtc = DateTime.UtcNow;

        var accessToken = _jwtService.GenerateAccessToken(
            user.Id,
            user.Email,
            role.Name);

        var rawRefreshToken = _refreshTokenService.GenerateToken();

        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = _refreshTokenService.HashToken(
                rawRefreshToken),
            UserId = user.Id,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        };

        _repository.AddRefreshToken(newRefreshToken);

        await _repository.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            UserId = user.Id,
            Role = role.Name
        };
    }

    public async Task LogoutAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = _refreshTokenService.HashToken(refreshToken);

        var storedToken = await _repository.GetRefreshTokenAsync(
            tokenHash,
            cancellationToken);

        if (storedToken is null)
        {
            return;
        }

        if (storedToken.RevokedAtUtc is not null)
        {
            return;
        }

        storedToken.RevokedAtUtc = DateTime.UtcNow;

        await _repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProfileResponse> GetProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetUserByIdAsync(
            userId,
            cancellationToken);

        if (user is null || user.IsDeleted)
        {
            throw new UnauthorizedAccessException(
                "User account was not found.");
        }

        var role = await _repository.GetRoleByIdAsync(
            user.RoleId,
            cancellationToken);

        if (role is null)
        {
            throw new InvalidOperationException(
                "User role was not found.");
        }

        var response = _mapper.Map<ProfileResponse>(user);

        response.Role = role.Name;

        return response;
    }
}