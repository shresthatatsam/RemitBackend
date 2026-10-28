
using EverestRemit.Api.Data;
using EverestRemit.Api.DTOs;
using EverestRemit.Api.Entities;
using EverestRemit.Api.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace EverestRemit.Api.Services;

public sealed class TransferService(
    ApplicationDbContext db,
    DepositFileStorage fileStorage) : ITransferService
{
    // Get transfers belonging to current user
    public async Task<IReadOnlyList<RemittanceTransfer>>
        GetMineAsync(string uid, CancellationToken ct)
    {
        return await db.Transfers
            .AsNoTracking()
            .Include(x => x.Beneficiary)
            .Where(x => x.UserId == uid)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(ct);
    }

    // Create remittance transfer + upload proof image
    public async Task<RemittanceTransfer> CreateAsync(
        string uid,
        CreateTransferRequest r,
        CancellationToken ct)
    {
        // Validate amount
        if (r.SendAmount <= 0 ||
            r.SendAmount > 100000000)
        {
            throw new ArgumentException(
                "Amount must be between 0.01 and 100000000.");
        }

        // Validate currency
        if (string.IsNullOrWhiteSpace(r.SendCurrency))
        {
            throw new ArgumentException(
                "Currency is required.");
        }

        var currency = r.SendCurrency
            .Trim()
            .ToUpperInvariant();

        if (currency.Length != 3 ||
            currency.Any(c => c < 'A' || c > 'Z'))
        {
            throw new ArgumentException(
                "Currency must be a three-letter code.");
        }

        // Verify beneficiary belongs to current user
        var beneficiary = await db.Beneficiaries
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == r.BeneficiaryId &&
                     x.UserId == uid,
                ct);

        if (beneficiary == null)
        {
            throw new InvalidOperationException(
                "Beneficiary not found.");
        }

        // TODO: Replace with your active exchange-rate lookup.
        // Do not trust an exchange rate supplied by the client.
        //decimal rate = 123m;
        var rate = await db.ExchangeRates.AsNoTracking()
            .Where(x => x.FromCurrency == currency && x.ToCurrency == "NPR" && x.IsActive)
            .OrderByDescending(x => x.EffectiveAtUtc).FirstOrDefaultAsync(ct);
        if (rate == null) throw new InvalidOperationException("No active exchange rate for this currency to NPR.");

        if (rate.Rate <= 0)
        {
            throw new InvalidOperationException(
                "No active exchange rate for this currency to NPR.");
        }

        // Save private proof image
        var fileKey = await fileStorage.SaveAsync(
            r.ProofImage,
            ct);

        try
        {
            var transfer = new RemittanceTransfer
            {
                UserId = uid,
                BeneficiaryId = beneficiary.Id,

                SendAmount = r.SendAmount,
                SendCurrency = currency,

                ExchangeRate = rate.Rate,

                ReceiveAmountNpr = decimal.Round(
                    r.SendAmount * rate.Rate,
                    2,
                    MidpointRounding.AwayFromZero),

                // Generated private storage key
                ProofUrl = fileKey,


                Status = TransferStatus.Pending,

                CreatedAtUtc = DateTime.UtcNow
            };

            db.Transfers.Add(transfer);

            await db.SaveChangesAsync(ct);

            return transfer;
        }
        catch
        {
            // Cleanup image if database save fails
            fileStorage.Delete(fileKey);
            throw;
        }
    }

    // Get all transfers for admin
    public async Task<IReadOnlyList<RemittanceTransfer>>
        GetAllAsync(CancellationToken ct)
    {
        return await db.Transfers
            .AsNoTracking()
            .Include(x => x.Beneficiary)
            .Include(x => x.User)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(ct);
    }

    // Admin updates transfer status
    public async Task<RemittanceTransfer?>
        UpdateStatusAsync(
            int id,
            UpdateTransferStatusRequest r,
            CancellationToken ct)
    {
        var transfer = await db.Transfers
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (transfer == null)
            return null;

        if (transfer.Status is TransferStatus.Paid
            or TransferStatus.Rejected
            or TransferStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "Finalised transfer cannot be changed.");
        }

        if (r.Status == TransferStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "Use Rejected for admin rejection.");
        }

        if (!Enum.IsDefined(typeof(TransferStatus), r.Status))
        {
            throw new ArgumentException(
                "Invalid transfer status.");
        }

        transfer.Status = r.Status;
        transfer.AdminNote = r.AdminNote?.Trim();
        transfer.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return transfer;
    }
}; 