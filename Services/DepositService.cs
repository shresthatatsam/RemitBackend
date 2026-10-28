
using EverestRemit.Api.Data;
using EverestRemit.Api.DTOs;
using EverestRemit.Api.Entities;
using EverestRemit.Api.Interfaces;
using EverestRemit.Api.Migrations;
using Microsoft.EntityFrameworkCore;

namespace EverestRemit.Api.Services;

public sealed class DepositService(
    ApplicationDbContext db,
    DepositFileStorage fileStorage) : IDepositService
{
    public async Task<IReadOnlyList<Deposit>> GetMineAsync(
        string uid,
        CancellationToken ct)
    {
        return await db.Deposits
            .AsNoTracking()
            .Where(x => x.UserId == uid && x.IsActive)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(ct);
    }

    public async Task<Deposit> CreateAsync(
        string uid,
        CreateDepositRequest r,
        CancellationToken ct)
    {
        if (r.Amount <= 0 || r.Amount > 100000000)
        {
            throw new ArgumentException(
                "Amount must be between 0.01 and 100000000.");
        }

        //var currency = r.Currency.Trim().ToUpperInvariant();

        //if (currency.Length != 3 ||
        //    currency.Any(c => c < 'A' || c > 'Z'))
        //{
        //    throw new ArgumentException(
        //        "Currency must be a three-letter code.");
        //}

        //var referenceNumber = r.ReferenceNumber.Trim();

        var referenceNumber =$"DEP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}".Substring(0, 17).ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(referenceNumber))
        {
            throw new ArgumentException(
                "Reference number is required.");
        }

        // Save the uploaded proof image to private storage.
        // The returned value is a generated file key.
        var fileKey = await fileStorage.SaveAsync(
            r.ProofImage,
            ct);

        var deposit = new Deposit
        {
            UserId = uid,
            Amount = r.Amount,
            ReferenceNumber = referenceNumber,
            ProofUrl = fileKey,
            Status = DepositStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        try
        {
            db.Deposits.Add(deposit);
            await db.SaveChangesAsync(ct);

            return deposit;
        }
        catch
        {
            // Remove the uploaded file if database persistence fails.
            fileStorage.Delete(fileKey);
            throw;
        }
    }

    public async Task<IReadOnlyList<Deposit>> GetAllAsync(
        CancellationToken ct)
    {
        return await db.Deposits
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(ct);
    }

    public async Task<Deposit?> ReviewAsync(
        int id,
        ReviewDepositRequest r,
        CancellationToken ct)
    {
        if (r.Status is not DepositStatus.Confirmed
            and not DepositStatus.Rejected)
        {
            throw new ArgumentException(
                "Status must be Confirmed or Rejected.");
        }

        var deposit = await db.Deposits.FindAsync([id], ct);

        if (deposit is null)
            return null;

        if (deposit.Status != DepositStatus.Pending)
        {
            throw new InvalidOperationException(
                "Only pending deposits can be reviewed.");
        }

        deposit.Status = r.Status;
        deposit.AdminNote = r.AdminNote?.Trim();
        deposit.ReviewedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return deposit;
    }
}
