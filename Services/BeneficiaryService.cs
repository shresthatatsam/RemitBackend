using EverestRemit.Api.Data;
using EverestRemit.Api.DTOs;
using EverestRemit.Api.Entities;
using EverestRemit.Api.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace EverestRemit.Api.Services;

public sealed class BeneficiaryService(ApplicationDbContext db) : IBeneficiaryService
{
    public async Task<IReadOnlyList<Beneficiary>> GetMineAsync(string uid, CancellationToken ct) =>
        await db.Beneficiaries.AsNoTracking().Where(x => x.UserId == uid).OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct);
    public async Task<Beneficiary> CreateAsync(string uid, BeneficiaryRequest r, CancellationToken ct)
    {
        var x = Map(new Beneficiary { UserId = uid }, r);
        db.Beneficiaries.Add(x);
        await db.SaveChangesAsync(ct);
        return x;
    }
    public async Task<Beneficiary?> UpdateAsync(string uid, int id, BeneficiaryRequest r, CancellationToken ct)
    {
        var x = await db.Beneficiaries.SingleOrDefaultAsync(x => x.Id == id && x.UserId == uid, ct);
        if (x == null)
            return null; Map(x, r);
        await db.SaveChangesAsync(ct);
        return x;
    }
    public async Task<bool> DeleteAsync(string uid, int id, CancellationToken ct)
    {
        var x = await db.Beneficiaries.SingleOrDefaultAsync(x => x.Id == id && x.UserId == uid, ct);
        if (x == null)
            return false;
        if (await db.Transfers.AnyAsync(t => t.BeneficiaryId == id, ct))
            throw new InvalidOperationException("Beneficiary is linked to a transfer and cannot be deleted.");
        db.Remove(x);
        await db.SaveChangesAsync(ct); return true;
    }
    static Beneficiary Map(Beneficiary x, BeneficiaryRequest r)
    {
        x.AccountName = r.AccountName.Trim();
        x.AccountNumber = r.AccountNumber.Trim();
        x.BankName = r.BankName.Trim();
        x.BankCountry = r.BankCountry.Trim();
        x.BankCode = r.BankCode?.Trim();
        x.BranchName = r.BranchName?.Trim();
        x.Currency = r.Currency.Trim().ToUpperInvariant();
        return x;
    }
}
