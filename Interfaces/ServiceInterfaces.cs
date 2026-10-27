using EverestRemit.Api.DTOs;
using EverestRemit.Api.Entities;
namespace EverestRemit.Api.Interfaces;

public interface IAuthService 
{ 
    Task<AuthResponse> RegisterAsync(RegisterRequest r, CancellationToken ct);
    Task<AuthResponse> LoginAsync(LoginRequest r, CancellationToken ct);
    Task<AuthResponse> RefreshAsync(
       RefreshRequest request,
       CancellationToken ct);

    Task LogoutAsync(
        RefreshRequest request,
        CancellationToken ct);

    Task ForgotPasswordAsync(
    ForgotPasswordRequest request,
    CancellationToken ct);

    Task ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken ct);
}
public interface IKycService
{

    Task<KycApplication?> GetMineAsync(string uid, CancellationToken ct);
    Task<IReadOnlyList<KycApplication>> GetAllAsync(CancellationToken ct);
    Task<KycApplication?> ReviewAsync(int id, ReviewKycRequest r, CancellationToken ct);
    Task<KycApplication> SavePersonalAsync(string userId, PersonalInformationRequest request, CancellationToken ct);
    Task<KycApplication> SaveDocumentAsync(string userId,IdentityDocumentRequest request,CancellationToken ct);
    Task<KycApplication> SaveSelfieAsync(string userId, SelfieRequest request, CancellationToken ct);
    Task<KycApplication> SubmitAsync(string userId, CancellationToken ct);
}
public interface IBeneficiaryService { Task<IReadOnlyList<Beneficiary>> GetMineAsync(string userId, CancellationToken ct); Task<Beneficiary> CreateAsync(string userId, BeneficiaryRequest r, CancellationToken ct); Task<Beneficiary?> UpdateAsync(string userId, int id, BeneficiaryRequest r, CancellationToken ct); Task<bool> DeleteAsync(string userId, int id, CancellationToken ct); }
public interface IExchangeRateService { Task<IReadOnlyList<ExchangeRate>> GetActiveAsync(CancellationToken ct); Task<ExchangeRate> CreateAsync(string adminId, ExchangeRateRequest r, CancellationToken ct); }
public interface IDepositService {
    Task<IReadOnlyList<Deposit>> GetMineAsync(
    string uid, CancellationToken ct);

    Task<Deposit> CreateAsync(
        string uid, CreateDepositRequest r, CancellationToken ct);

    Task<IReadOnlyList<Deposit>> GetAllAsync(
        CancellationToken ct);

    Task<Deposit?> ReviewAsync(
        int id, ReviewDepositRequest r, CancellationToken ct);
}
public interface ITransferService { Task<IReadOnlyList<RemittanceTransfer>> GetMineAsync(string userId, CancellationToken ct); Task<RemittanceTransfer> CreateAsync(string userId, CreateTransferRequest r, CancellationToken ct); Task<IReadOnlyList<RemittanceTransfer>> GetAllAsync(CancellationToken ct); Task<RemittanceTransfer?> UpdateStatusAsync(int id, UpdateTransferStatusRequest r, CancellationToken ct); }
