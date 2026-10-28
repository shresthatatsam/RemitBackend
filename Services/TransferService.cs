using EverestRemit.Api.Data;
using EverestRemit.Api.DTOs;
using EverestRemit.Api.Entities;
using EverestRemit.Api.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace EverestRemit.Api.Services;

public sealed class TransferService(ApplicationDbContext db) : ITransferService
{
    public async Task<IReadOnlyList<RemittanceTransfer>> GetMineAsync(string uid, CancellationToken ct) => 
        await db.Transfers.AsNoTracking().Include(x => x.Beneficiary).Include(x=>x.Deposit).Where(x => x.UserId == uid).OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct);
    public async Task<RemittanceTransfer> CreateAsync(string uid, CreateTransferRequest r, CancellationToken ct)
    {
        //var k = await db.KycApplications.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == uid, ct);
        //if (k?.Status != KycStatus.Approved)
        //    throw new InvalidOperationException("KYC must be approved before transfer.");
        var b = await db.Beneficiaries.AsNoTracking().SingleOrDefaultAsync(x => x.Id == r.BeneficiaryId && x.UserId == uid, ct);
        if (b == null) throw new InvalidOperationException("Beneficiary not found.");
        var currency = r.SendCurrency.Trim().ToUpperInvariant(); if (r.DepositId.HasValue)
        {
            var d = await db.Deposits.AsNoTracking().SingleOrDefaultAsync(x => x.Id == r.DepositId && x.UserId == uid, ct);
            if (d == null || d.Currency != currency || d.Amount < r.SendAmount)
                throw new InvalidOperationException("Confirmed deposit in same currency with sufficient amount required.");
        }
        //var rate = await db.ExchangeRates.AsNoTracking()
        //    .Where(x => x.FromCurrency == currency && x.ToCurrency == "NPR" && x.IsActive)
        //    .OrderByDescending(x => x.EffectiveAtUtc).FirstOrDefaultAsync(ct);
        decimal rate = 123;
        if (rate == null)
            throw new InvalidOperationException("No active exchange rate for this currency to NPR.");
        var x = new RemittanceTransfer
        {
            UserId = uid,
            BeneficiaryId = b.Id,
            DepositId = r.DepositId,
            SendAmount = r.SendAmount,
            SendCurrency = currency,
            ExchangeRate = rate,
            ReceiveAmountNpr = decimal.Round(r.SendAmount * rate, 2, MidpointRounding.AwayFromZero)
        };
        db.Add(x);
        await db.SaveChangesAsync(ct); return x;
    }
    public async Task<IReadOnlyList<RemittanceTransfer>> GetAllAsync(CancellationToken ct) =>
        await db.Transfers.AsNoTracking()
        .Include(x => x.Beneficiary)
        .Include(x => x.User)
        .Include(x=>x.Deposit).OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct);
    public async Task<RemittanceTransfer?> UpdateStatusAsync(int id, UpdateTransferStatusRequest r, CancellationToken ct) { var x = await db.Transfers.FindAsync([id], ct); if (x == null) return null; if (x.Status is TransferStatus.Paid or TransferStatus.Rejected or TransferStatus.Cancelled) throw new InvalidOperationException("Finalised transfer cannot be changed."); if (r.Status == TransferStatus.Cancelled) throw new InvalidOperationException("Use Rejected for admin rejection."); x.Status = r.Status; x.AdminNote = r.AdminNote; x.UpdatedAtUtc = DateTime.UtcNow; await db.SaveChangesAsync(ct); return x; }
}
