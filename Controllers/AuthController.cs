using EverestRemit.Api.DTOs;
using EverestRemit.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;
namespace EverestRemit.Api.Controllers;

[ApiController, Route("api/auth")]
public sealed class AuthController(IAuthService s) : ControllerBase
{
    [HttpPost("register")] public async Task<IActionResult> Register(RegisterRequest r, CancellationToken ct) { try { return Ok(await s.RegisterAsync(r, ct)); } catch (InvalidOperationException e) { return Conflict(new { message = e.Message }); } }
    [HttpPost("login")] public async Task<IActionResult> Login(LoginRequest r, CancellationToken ct) { try { return Ok(await s.LoginAsync(r, ct)); } catch (UnauthorizedAccessException) { return Unauthorized(new { message = "Invalid email or password." }); } }


    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        RefreshRequest r, CancellationToken ct)
    {
        try
        {
            return Ok(await s.RefreshAsync(r, ct));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new
            {
                message = "Invalid or expired refresh token."
            });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        RefreshRequest r, CancellationToken ct)
    {
        await s.LogoutAsync(r, ct);

        return NoContent();
    }
}