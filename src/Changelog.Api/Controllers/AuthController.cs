using Changelog.Api.Auth;
using Changelog.Api.Data;
using Changelog.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Changelog.Api.Controllers;

[ApiController]
[AllowAnonymous]
public sealed class AuthController(UserRepository users, TokenService tokens) : ControllerBase
{
    private const int BcryptWorkFactor = 11;

    [HttpGet("/")]
    public MessageResponse Hello() => new() { Message = "hello" };

    /// <summary>Create an account and return a token for it.</summary>
    [HttpPost("/user")]
    public async Task<ActionResult<TokenResponse>> Register(AuthRequest request, CancellationToken ct)
    {
        try
        {
            var hash = BCrypt.Net.BCrypt.HashPassword(request.Password, BcryptWorkFactor);
            var user = await users.CreateAsync(request.Username.Trim(), hash, ct);
            return new TokenResponse { Token = tokens.CreateToken(user) };
        }
        catch (DuplicateUsernameException)
        {
            return BadRequest(new MessageResponse { Message = "username is already taken" });
        }
    }

    [HttpPost("/signin")]
    public async Task<ActionResult<TokenResponse>> SignIn(AuthRequest request, CancellationToken ct)
    {
        var user = await users.FindByUsernameAsync(request.Username.Trim(), ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new MessageResponse { Message = "invalid username or password" });

        return new TokenResponse { Token = tokens.CreateToken(user) };
    }
}
