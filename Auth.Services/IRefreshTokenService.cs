namespace Auth.Services;

public interface IRefreshTokenService
{
    string GenerateToken();

    string HashToken(string token);
}