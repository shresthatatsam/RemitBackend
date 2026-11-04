using EverestRemit.Api.DTOs;
using EverestRemit.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EverestRemit.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService s) : ControllerBase
{
    // POST: api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        RegisterRequest r,
        CancellationToken ct)
    {
        try
        {
            var result = await s.RegisterAsync(r, ct);

            return Ok(ApiResponse<object>.Ok(
                result,
                "Registration successful"));
        }
        catch (InvalidOperationException e)
        {
            return Conflict(
                ApiResponse<object>.Fail(e.Message));
        }
    }

    // POST: api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        LoginRequest r,
        CancellationToken ct)
    {
        try
        {
            var result = await s.LoginAsync(r, ct);

            return Ok(ApiResponse<object>.Ok(
                result,
                "Login successful"));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(
                ApiResponse<object>.Fail(
                    "Invalid email or password."));
        }
    }

    // POST: api/auth/refresh
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        RefreshRequest r,
        CancellationToken ct)
    {
        try
        {
            var result = await s.RefreshAsync(r, ct);

            return Ok(ApiResponse<object>.Ok(
                result,
                "Token refreshed successfully"));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(
                ApiResponse<object>.Fail(
                    "Invalid or expired refresh token."));
        }
    }

    // POST: api/auth/logout
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        RefreshRequest r,
        CancellationToken ct)
    {
        await s.LogoutAsync(r, ct);

        return Ok(ApiResponse<object>.Ok(
            null!,
            "Logged out successfully"));
    }

    // POST: api/auth/forgot-password
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        ForgotPasswordRequest request,
        CancellationToken ct)
    {
        await s.ForgotPasswordAsync(request, ct);

        return Ok(ApiResponse<object>.Ok(
            null!,
            "If the email is registered, a password reset link will be sent."));
    }

    // POST: api/auth/reset-password
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordRequest request,
        CancellationToken ct)
    {
        try
        {
            await s.ResetPasswordAsync(request, ct);

            return Ok(ApiResponse<object>.Ok(
                null!,
                "Password reset successfully. Please log in again."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(
                ApiResponse<object>.Fail(ex.Message));
        }
    }
}