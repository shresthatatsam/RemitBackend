
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
    public async Task<IReadOnlyList<RemittanceTransfer>>GetMineAsync(string uid, CancellationToken ct)
    {
        return await db.Transfers
            .AsNoTracking()
            .Include(x => x.Beneficiary)
            .Where(x => x.UserId == uid)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(ct);
    }

    // Create remittance transfer + upload proof image
    public async Task<RemittanceTransfer> CreateAsync(string uid,CreateTransferRequest r,CancellationToken ct)
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
    public async Task<PaginatedResponse<TransferResponse>> GetMineAsync(
    string uid,
    int pageNumber,
    int pageSize,
    CancellationToken ct)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Transfers
            .AsNoTracking()
            .Where(x => x.UserId == uid);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Select(x => new TransferResponse
            {
                Id = x.Id,
                TransferNumber = x.TransferNumber,

                UserId = x.UserId,

                BeneficiaryId = x.BeneficiaryId,
                BeneficiaryName = x.Beneficiary != null
                    ? x.Beneficiary.AccountName : null,
                AccountNumber = x.Beneficiary != null
                    ? x.Beneficiary.AccountNumber : null,
                BankName = x.Beneficiary != null
                    ? x.Beneficiary.BankName : null,
                BankCountry = x.Beneficiary != null
                    ? x.Beneficiary.BankCountry : null,

                ProofUrl = x.ProofUrl,

                SendAmount = x.SendAmount,
                SendCurrency = x.SendCurrency,
                ExchangeRate = x.ExchangeRate,
                ReceiveAmountNpr = x.ReceiveAmountNpr,

                Status = x.Status,
                AdminNote = x.AdminNote,

                CreatedAtUtc = x.CreatedAtUtc,
                UpdatedAtUtc = x.UpdatedAtUtc
            })
            .ToListAsync(ct);

        return new PaginatedResponse<TransferResponse>
        {
            Items = items,
            Pagination = new PaginationMetadata
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            }
            
        };
    }

    public async Task<PaginatedResponse<TransferResponse>> GetAllAsync(
    int pageNumber,
    int pageSize,
    CancellationToken ct)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Transfers.AsNoTracking();

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Select(x => new TransferResponse
            {
                Id = x.Id,
                TransferNumber = x.TransferNumber,

                UserId = x.UserId,
                UserName = x.User != null ? x.User.UserName : null,
                UserEmail = x.User != null ? x.User.Email : null,

                BeneficiaryId = x.BeneficiaryId,
                BeneficiaryName = x.Beneficiary != null
                    ? x.Beneficiary.AccountName : null,
                AccountNumber = x.Beneficiary != null
                    ? x.Beneficiary.AccountNumber : null,
                BankName = x.Beneficiary != null
                    ? x.Beneficiary.BankName : null,
                BankCountry = x.Beneficiary != null
                    ? x.Beneficiary.BankCountry : null,

                ProofUrl = x.ProofUrl,

                SendAmount = x.SendAmount,
                SendCurrency = x.SendCurrency,
                ExchangeRate = x.ExchangeRate,
                ReceiveAmountNpr = x.ReceiveAmountNpr,

                Status = x.Status,
                AdminNote = x.AdminNote,

                CreatedAtUtc = x.CreatedAtUtc,
                UpdatedAtUtc = x.UpdatedAtUtc
            })
            .ToListAsync(ct);

        return new PaginatedResponse<TransferResponse>
        {
            Items = items,
            Pagination = new PaginationMetadata
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            }
        };
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


    public async Task<TransferResponse?> GetByIdAsync(
        int id,
        string? uid,
        bool isAdmin,
        CancellationToken ct)
    {
        var query = db.Transfers
            .AsNoTracking()
            .Where(x => x.Id == id);

        if (!isAdmin)
            query = query.Where(x => x.UserId == uid);

        return await query
            .Select(x => new TransferResponse
            {
                Id = x.Id,
                TransferNumber = x.TransferNumber,
                UserId = x.UserId,

                BeneficiaryId = x.BeneficiaryId,
                BeneficiaryName = x.Beneficiary != null
                    ? x.Beneficiary.AccountName : null,

                ProofUrl = x.ProofUrl,

                SendAmount = x.SendAmount,
                SendCurrency = x.SendCurrency,
                ExchangeRate = x.ExchangeRate,
                ReceiveAmountNpr = x.ReceiveAmountNpr,

                Status = x.Status,
                AdminNote = x.AdminNote,
                CreatedAtUtc = x.CreatedAtUtc,
                UpdatedAtUtc = x.UpdatedAtUtc
            })
            .SingleOrDefaultAsync(ct);
    }

}; 