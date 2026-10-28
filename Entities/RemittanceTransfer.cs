namespace EverestRemit.Api.Entities;

public enum TransferStatus { Pending, Processing, Paid, Rejected, Cancelled }
public sealed class RemittanceTransfer
{
    public int Id { get; set; }
    public string TransferNumber { get; set; } = Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
    public string UserId { get; set; } = "";
    public ApplicationUser? User { get; set; }
    public int BeneficiaryId { get; set; }
    public Beneficiary? Beneficiary { get; set; }
    public int? DepositId { get; set; }
    public Deposit? Deposit { get; set; }
    public decimal SendAmount { get; set; }
    public string SendCurrency { get; set; } = "USD";
    public decimal ExchangeRate { get; set; }
    public decimal ReceiveAmountNpr { get; set; }
    public TransferStatus Status { get; set; } = TransferStatus.Pending;
    public string? AdminNote { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}