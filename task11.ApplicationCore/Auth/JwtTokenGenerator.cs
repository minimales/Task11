using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using task11.ApplicationCore.Entities;

namespace task11.ApplicationCore.Auth;

public class JwtTokenGenerator
{
    private readonly JwtSettings _settings;
    private readonly IClock _clock;

    public JwtTokenGenerator(IOptions<JwtSettings> settings, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(clock);

        _settings = settings.Value;
        _clock = clock;
    }

    public (string Token, DateTime ExpiresAtUtc) Generate(UserEntity user)
    {
        ArgumentNullException.ThrowIfNull(user);

        DateTime now = _clock.UtcNow;
        DateTime expires = now.AddMinutes(_settings.ExpiryMinutes);

        Claim[] claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("name", user.Username),
            new Claim("role", user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        SigningCredentials credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new JwtSecurityToken(
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
