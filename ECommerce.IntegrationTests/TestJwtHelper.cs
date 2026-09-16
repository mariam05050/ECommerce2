using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ECommerce.IntegrationTests;

public static class TestJwtHelper
{
    private static readonly IConfiguration Configuration =
        new ConfigurationBuilder()
            .AddUserSecrets<TestSettings>()
            .Build();

    public static string CreateToken(
        Guid userId,
        string email = "test@example.com",
        string role = "Customer")
    {
        var keyValue = Configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(keyValue))
        {
            throw new InvalidOperationException(
                "JWT key was not found in User Secrets.");
        }

        var claims = new List<Claim>
        {
            new(
                ClaimTypes.NameIdentifier,
                userId.ToString()),

            new(
                JwtRegisteredClaimNames.Email,
                email),

            new(
                ClaimTypes.Role,
                role)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(keyValue));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "ECommerce2",
            audience: "ECommerce2",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}