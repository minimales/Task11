using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using task11.Data;
using task11.Data.Entities;

namespace task11.ApplicationCore.Auth;

/// <summary>
/// Issues HS256 JWTs with <c>sub</c>, <c>name</c>, <c>role</c>, <c>jti</c> and <c>exp</c> claims.
/// </summary>
public sealed class JwtTokenGenerator
{
    private readonly JwtSettings _settings;
    private readonly IClock _clock;

    public JwtTokenGenerator(IOptions<JwtSettings> settings, IClock clock)
    {
        _settings = settings.Value;
        _clock = clock;
    }

    /// <summary>Generates a signed JWT for the given user and returns the token and its UTC expiry.</summary>
    public (string Token, DateTime ExpiresAtUtc) Generate(UserEntity user)
    {
        ArgumentNullException.ThrowIfNull(user);

        DateTime now = _clock.UtcNow;
        DateTime expires = now.AddMinutes(_settings.ExpiryMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("name", user.Username),
            new Claim("role", user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        string encoded = new JwtSecurityTokenHandler().WriteToken(token);
        return (encoded, expires);
    }
}
