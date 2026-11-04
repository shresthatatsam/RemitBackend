using EverestRemit.Api.Entities;

namespace EverestRemit.Api.DTOs
{
    public sealed class TransferResponse
    {
        public int Id { get; set; }
        public string TransferNumber { get; set; } = "";

        public string UserId { get; set; } = "";
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }

        public int BeneficiaryId { get; set; }
        public string? BeneficiaryName { get; set; }
        public string? AccountNumber { get; set; }
        public string? BankName { get; set; }
        public string? BankCountry { get; set; }

        public string? ProofUrl { get; set; }

        public decimal SendAmount { get; set; }
        public string SendCurrency { get; set; } = "";

        public decimal ExchangeRate { get; set; }
        public decimal ReceiveAmountNpr { get; set; }

        public TransferStatus Status { get; set; }
        public string? AdminNote { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
