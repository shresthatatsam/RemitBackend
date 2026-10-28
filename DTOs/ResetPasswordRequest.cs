namespace EverestRemit.Api.DTOs
{
    public sealed record ResetPasswordRequest(
     string Email,
     string Otp,
     string NewPassword);
}
