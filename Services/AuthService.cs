


using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EverestRemit.Api.Data;
using EverestRemit.Api.DTOs;
using EverestRemit.Api.Entities;
using EverestRemit.Api.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace EverestRemit.Api.Services;

public sealed class AuthService(
    UserManager<ApplicationUser> users,
    ApplicationDbContext db,
    IConfiguration config,
    IEmailService emailService) : IAuthService
{
    private static readonly TimeSpan RefreshLifetime =
        TimeSpan.FromDays(7);

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest r,
        CancellationToken ct)
    {
        var email = r.Email.Trim();

        var u = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = r.FullName.Trim()
        };

        var result = await users.CreateAsync(u, r.Password);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join("; ",
                    result.Errors.Select(e => e.Description)));
        }

        // Public registration must never grant Admin privileges.
        var roleResult = await users.AddToRoleAsync(u, "User");

        if (!roleResult.Succeeded)
        {
            await users.DeleteAsync(u);

            throw new InvalidOperationException(
                string.Join("; ",
                    roleResult.Errors.Select(e => e.Description)));
        }

        return await CreateTokensAsync(u, ct);
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest r,
        CancellationToken ct)
    {
        var u = await users.FindByEmailAsync(r.Email.Trim());

        if (u is null ||
            !u.IsActive ||
            !await users.CheckPasswordAsync(u, r.Password))
        {
            throw new UnauthorizedAccessException();
        }

        return await CreateTokensAsync(u, ct);
    }

    public async Task<AuthResponse> RefreshAsync(
        RefreshRequest r,
        CancellationToken ct)
    {
        var hash = HashToken(r.RefreshToken);

        var oldToken = await db.RefreshTokens
            .Include(x => x.User)
            .SingleOrDefaultAsync(
                x => x.TokenHash == hash, ct);

        if (oldToken is null ||
            !oldToken.IsActive ||
            !oldToken.User.IsActive)
        {
            throw new UnauthorizedAccessException();
        }

        // Rotate the refresh token: revoke the old one and
        // issue a new pair of tokens.
        oldToken.RevokedAtUtc = DateTime.UtcNow;

        var response = await CreateTokensAsync(
            oldToken.User, ct);

        // Find the replacement by hashing the newly issued token.
        var replacementHash = HashToken(response.RefreshToken);

        var replacementId = await db.RefreshTokens
            .Where(x => x.TokenHash == replacementHash)
            .Select(x => x.Id)
            .SingleAsync(ct);

        oldToken.ReplacedByTokenId = replacementId;

        await db.SaveChangesAsync(ct);

        return response;
    }

    public async Task LogoutAsync(
        RefreshRequest r,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.RefreshToken))
            return;

        var hash = HashToken(r.RefreshToken);

        var token = await db.RefreshTokens
            .SingleOrDefaultAsync(
                x => x.TokenHash == hash, ct);

        if (token is not null && token.RevokedAtUtc is null)
        {
            token.RevokedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task<AuthResponse> CreateTokensAsync(
        ApplicationUser u,
        CancellationToken ct)
    {
        var roles = (await users.GetRolesAsync(u)).ToArray();

        var now = DateTime.UtcNow;

        var expiryMinutes = Math.Clamp(
            config.GetValue<int?>("Jwt:ExpiryMinutes") ?? 15,
            5, 1440);

        var accessExpiry = now.AddMinutes(expiryMinutes);

        var secret = config["Jwt:Secret"];

        if (string.IsNullOrWhiteSpace(secret) ||
            Encoding.UTF8.GetByteCount(secret) < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Secret must be configured with at least 32 UTF-8 bytes.");
        }

        var issuer = config["Jwt:Issuer"] ?? "EverestRemit";
        var audience = config["Jwt:Audience"] ?? "EverestRemitUsers";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, u.Id),
            new(ClaimTypes.Name, u.FullName),
            new(JwtRegisteredClaimNames.Email, u.Email ?? ""),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(
            roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secret));

        var jwt = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            now,
            accessExpiry,
            new SigningCredentials(
                key, SecurityAlgorithms.HmacSha256));

        var accessToken =
            new JwtSecurityTokenHandler().WriteToken(jwt);

        var rawRefreshToken = GenerateRefreshToken();
        var refreshExpiry = now.Add(RefreshLifetime);

        var refreshToken = new RefreshToken
        {
            UserId = u.Id,
            TokenHash = HashToken(rawRefreshToken),
            CreatedAtUtc = now,
            ExpiresAtUtc = refreshExpiry
        };

        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync(ct);

        return new AuthResponse(
            accessToken,
            accessExpiry,
            u.Id,
            u.Email ?? "",
            u.FullName,
            roles,
            rawRefreshToken,
            refreshExpiry);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request,CancellationToken ct)
{
    var email = request.Email.Trim();
    var user = await users.FindByEmailAsync(email);

    // Never reveal whether an account exists.
    if (user is null || !user.IsActive)
        return;

    // Invalidate previous unused OTPs for this user.
    var previousOtps = await db.PasswordResetOtps
        .Where(x => x.UserId == user.Id &&
                    x.UsedAtUtc == null)
        .ToListAsync(ct);

    foreach (var previous in previousOtps)
        previous.UsedAtUtc = DateTime.UtcNow;

    var otp = RandomNumberGenerator
        .GetInt32(0, 1_000_000)
        .ToString("D6");

    var otpRecord = new PasswordResetOtp
    {
        Id = Guid.NewGuid(),
        UserId = user.Id,
        OtpHash = HashOtp(otp),
        CreatedAtUtc = DateTime.UtcNow,
        ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5),
        FailedAttempts = 0
    };

    db.PasswordResetOtps.Add(otpRecord);
    await db.SaveChangesAsync(ct);

    var html = $"""
        <div style="font-family:Arial,sans-serif;max-width:600px;margin:auto">
            <h2>Reset your EverestRemit password</h2>
            <p>Use the following verification code to reset your password:</p>
            <h1 style="letter-spacing:8px">{otp}</h1>
            <p>This code expires in 5 minutes.</p>
            <p>If you did not request this, you can ignore this email.</p>
            <p>Never share this code with anyone.</p>
            <p>EverestRemit Support</p>
        </div>
        """;

    await emailService.SendAsync(
        user.Email!,
        "Your password reset verification code",
        html,
        ct);
}

private string HashOtp(string otp)
{
    // Configure PasswordReset:OtpPepper as a strong random secret.
    var pepper = config["PasswordReset:OtpPepper"];

    if (string.IsNullOrWhiteSpace(pepper))
        throw new InvalidOperationException(
            "PasswordReset:OtpPepper must be configured.");

    var key = Encoding.UTF8.GetBytes(pepper);
    var data = Encoding.UTF8.GetBytes(otp);

    return Convert.ToHexString(
        HMACSHA256.HashData(key, data));
}

    public async Task ResetPasswordAsync(
    ResetPasswordRequest request,
    CancellationToken ct)
    {
        var email = request.Email.Trim();

        var user = await users.FindByEmailAsync(email);

        if (user is null || !user.IsActive)
        {
            throw new InvalidOperationException(
                "The verification code is invalid or expired.");
        }

        var now = DateTime.UtcNow;

        // Get the latest unused OTP.
        var otpRecord = await db.PasswordResetOtps
            .Where(x => x.UserId == user.Id &&
                        x.UsedAtUtc == null)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (otpRecord is null ||
            otpRecord.ExpiresAtUtc <= now ||
            otpRecord.FailedAttempts >= 5)
        {
            throw new InvalidOperationException(
                "The verification code is invalid or expired.");
        }

        var suppliedHash = HashOtp(request.Otp);

        if (!CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(otpRecord.OtpHash),
            Convert.FromHexString(suppliedHash)))
        {
            otpRecord.FailedAttempts++;
            await db.SaveChangesAsync(ct);

            throw new InvalidOperationException(
                "The verification code is invalid or expired.");
        }

        // Consume the OTP before changing the password.
        otpRecord.UsedAtUtc = now;
        await db.SaveChangesAsync(ct);

        var result = await users.ResetPasswordAsync(
            user,
            await users.GeneratePasswordResetTokenAsync(user),
            request.NewPassword);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join("; ",
                    result.Errors.Select(e => e.Description)));
        }

        // Revoke active refresh tokens after a successful reset.
        var activeTokens = await db.RefreshTokens
            .Where(x => x.UserId == user.Id &&
                        x.RevokedAtUtc == null &&
                        x.ExpiresAtUtc > now)
            .ToListAsync(ct);

        foreach (var token in activeTokens)
            token.RevokedAtUtc = now;

        await db.SaveChangesAsync(ct);
    }


    //public async Task ForgotPasswordAsync(ForgotPasswordRequest request,CancellationToken ct)
    //{
    //    var email = request.Email.Trim();
    //    var user = await users.FindByEmailAsync(email);

    //    // Do not reveal whether an email is registered.
    //    if (user is null || !user.IsActive)
    //        return;

    //    var token = await users.GeneratePasswordResetTokenAsync(user);

    //    var frontendUrl = config["PasswordReset:FrontendUrl"];

    //    if (string.IsNullOrWhiteSpace(frontendUrl) ||
    //        !Uri.TryCreate(frontendUrl, UriKind.Absolute, out var uri) ||
    //        (uri.Scheme != Uri.UriSchemeHttps &&
    //         uri.Scheme != Uri.UriSchemeHttp))
    //    {
    //        throw new InvalidOperationException(
    //            "PasswordReset:FrontendUrl must be configured.");
    //    }

    //    var resetUrl =
    //        $"{frontendUrl.TrimEnd('/')}/reset-password" +
    //        $"?email={Uri.EscapeDataString(user.Email!)}" +
    //        $"&token={Uri.EscapeDataString(token)}";

    //    var html = $"""
    //    <div style="font-family:Arial,sans-serif;max-width:600px;margin:auto">
    //        <h2>Reset your EverestRemit password</h2>
    //        <p>We received a request to reset your password.</p>
    //        <p>Click the button below to choose a new password.</p>
    //        <p>
    //            <a href="{System.Net.WebUtility.HtmlEncode(resetUrl)}"
    //               style="background:#166534;color:white;padding:12px 20px;
    //                      text-decoration:none;border-radius:6px;
    //                      display:inline-block">
    //                Reset Password
    //            </a>
    //        </p>
    //        <p>If you did not request this, you can ignore this email.</p>
    //        <p>For your security, do not share this link with anyone.</p>
    //        <p>EverestRemit Support</p>
    //    </div>
    //    """;

    //    await emailService.SendAsync(
    //        user.Email!,
    //        "Reset your EverestRemit password",
    //        html,
    //        ct);
    //}


    //public async Task ResetPasswordAsync(ResetPasswordRequest request,CancellationToken ct)
    //{
    //    var user = await users.FindByEmailAsync(
    //        request.Email.Trim());

    //    if (user is null || !user.IsActive)
    //    {
    //        throw new InvalidOperationException(
    //            "The password reset link is invalid or expired.");
    //    }

    //    var result = await users.ResetPasswordAsync(
    //        user,
    //        request.Token,
    //        request.NewPassword);

    //    if (!result.Succeeded)
    //    {
    //        throw new InvalidOperationException(
    //            string.Join("; ",
    //                result.Errors.Select(e => e.Description)));
    //    }

    //    // Revoke all existing refresh tokens after a password change.
    //    var now = DateTime.UtcNow;

    //    var activeTokens = await db.RefreshTokens
    //        .Where(x => x.UserId == user.Id &&
    //                    x.RevokedAtUtc == null &&
    //                    x.ExpiresAtUtc > now)
    //        .ToListAsync(ct);

    //    foreach (var refreshToken in activeTokens)
    //        refreshToken.RevokedAtUtc = now;

    //    await db.SaveChangesAsync(ct);
    //}


    private static string GenerateRefreshToken()
    {
        return Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(64))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}

