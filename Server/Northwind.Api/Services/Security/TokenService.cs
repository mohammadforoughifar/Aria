using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Northwind.Domain.Entities.Identity;

namespace Northwind.Api.Services.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>کلیدِ امضا — در محیطِ واقعی از متغیرِ محیطی یا Key Vault.</summary>
    public string Key { get; set; } = "northwind-dev-signing-key-change-me-in-production-0123456789";

    public string Issuer { get; set; } = "northwind";
    public string Audience { get; set; } = "northwind-client";

    /// <summary>مدتِ اعتبارِ توکن به دقیقه.</summary>
    public int ExpiryMinutes { get; set; } = 60 * 12;

    public SymmetricSecurityKey SigningKey => new(Encoding.UTF8.GetBytes(Key));
}

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) Create(User user);
}

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(JwtOptions options) => _options = options;

    public (string Token, DateTime ExpiresAt) Create(User user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(_options.SigningKey, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
