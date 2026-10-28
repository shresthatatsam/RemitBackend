using System.ComponentModel.DataAnnotations;
using EverestRemit.Api.Entities;
namespace EverestRemit.Api.DTOs;

public sealed class RegisterRequest { [Required, StringLength(120, MinimumLength = 2)] public string FullName { get; init; } = ""; [Required, EmailAddress] public string Email { get; init; } = ""; [Required, MinLength(10)] public string Password { get; init; } = ""; }
public sealed class LoginRequest { [Required, EmailAddress] public string Email { get; init; } = ""; [Required] public string Password { get; init; } = ""; }

public sealed record AuthResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    string UserId,
    string Email,
    string FullName,
    string[] Roles,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc
);
public sealed class SubmitKycRequest { [Required] public string FullName { get; init; } = ""; public DateOnly DateOfBirth { get; init; } [Required] public string Nationality { get; init; } = ""; [Required] public string DocumentType { get; init; } = ""; [Required] public string DocumentNumber { get; init; } = ""; public string? DocumentFrontUrl { get; init; } public string? DocumentBackUrl { get; init; } public string? SelfieUrl { get; init; } public string? Address { get; init; } }
public sealed class ReviewKycRequest { [Required] public KycStatus Status { get; init; } public string? AdminNote { get; init; } }
public sealed class BeneficiaryRequest { [Required] public string AccountName { get; init; } = ""; [Required] public string AccountNumber { get; init; } = ""; [Required] public string BankName { get; init; } = ""; [Required] public string BankCountry { get; init; } = "Nepal"; public string? BankCode { get; init; } public string? BranchName { get; init; } [StringLength(3, MinimumLength = 3)] public string Currency { get; init; } = "NPR"; }
public sealed class ExchangeRateRequest { [Required, StringLength(3, MinimumLength = 3)] public string FromCurrency { get; init; } = "USD"; [Required, StringLength(3, MinimumLength = 3)] public string ToCurrency { get; init; } = "NPR"; [Range(typeof(decimal), "0.000001", "100000000")] public decimal Rate { get; init; } public bool IsActive { get; init; } = true; }
public sealed class CreateDepositRequest
{
    [Range(typeof(decimal), "0.01", "100000000")]
    public decimal Amount { get; init; }
    [Required]
    public IFormFile ProofImage { get; init; } = null!;
}
public sealed class ReviewDepositRequest { public DepositStatus Status { get; init; } public string? AdminNote { get; init; } }
public sealed class CreateTransferRequest
{
    [Range(1, int.MaxValue)]
    public int BeneficiaryId { get; init; }
    [Range(typeof(decimal), "0.01", "100000000")]
    public decimal SendAmount { get; init; }
    [Required, StringLength(3, MinimumLength = 3)]
    public string SendCurrency { get; init; } = "USD";
    [Required]
    public IFormFile ProofImage { get; init; } = null!;
}
public sealed class UpdateTransferStatusRequest
{ public TransferStatus Status { get; init; } public string? AdminNote { get; init; } }
