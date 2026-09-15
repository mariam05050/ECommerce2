namespace Auth.Data;

public interface IAuthRepository
{
    Task<User?> GetUserByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<User?> GetUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Role?> GetRoleByNameAsync(
        string name,
        CancellationToken cancellationToken = default);

    Task<Role?> GetRoleByIdAsync(
        Guid roleId,
        CancellationToken cancellationToken = default);

    Task<RefreshToken?> GetRefreshTokenAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    void AddUser(User user);

    void AddRefreshToken(RefreshToken refreshToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}