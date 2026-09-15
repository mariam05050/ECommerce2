namespace Auth.Services;

public interface IJwtService
{
    string GenerateAccessToken(
        Guid userId,
        string email,
        string role);
}