using System.Security.Claims;
using System.Text;
using Changelog.Api.Data;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Changelog.Api.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>HMAC-SHA256 signing key; must be at least 32 characters.</summary>
    public string Secret { get; set; } = "";
    public string Issuer { get; set; } = "changelog-api";
    public string Audience { get; set; } = "changelog-clients";
    public TimeSpan Lifetime { get; set; } = TimeSpan.FromDays(7);

    public SymmetricSecurityKey SigningKey()
    {
        if (Secret.Length < 32)
            throw new InvalidOperationException("Jwt:Secret must be configured and at least 32 characters long.");
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
    }
}

public sealed class TokenService(IOptions<JwtOptions> options, TimeProvider clock)
{
    private readonly JwtOptions _options = options.Value;

    public string CreateToken(UserRecord user)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            ]),
            NotBefore = now,
            IssuedAt = now,
            Expires = now.Add(_options.Lifetime),
            SigningCredentials = new SigningCredentials(_options.SigningKey(), SecurityAlgorithms.HmacSha256),
        });
    }
}

public static class ClaimsPrincipalExtensions
{
    /// <summary>The authenticated user's id (the JWT "sub" claim).</summary>
    public static Guid GetUserId(this ClaimsPrincipal principal) =>
        Guid.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new InvalidOperationException("Authenticated principal has no 'sub' claim."));
}
