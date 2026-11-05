using EverestRemit.Api.Data;
using EverestRemit.Api.DTOs;
using EverestRemit.Api.Entities;
using EverestRemit.Api.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace EverestRemit.Api.Services;

public sealed class ExchangeRateService(ApplicationDbContext db) : IExchangeRateService
{
    public async Task<IReadOnlyList<ExchangeRate>> GetActiveAsync(CancellationToken ct) => await db.ExchangeRates.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.FromCurrency).ToListAsync(ct);
    public async Task<ExchangeRate> CreateAsync(string adminId, ExchangeRateRequest r, CancellationToken ct)
    {
        var from = r.FromCurrency.Trim().ToUpperInvariant();
        var to = r.ToCurrency.Trim().ToUpperInvariant();
        if (from == to)
            throw new InvalidOperationException("Currencies must differ.");
        var old = await db.ExchangeRates.
            Where(x => x.FromCurrency == from &&
            x.ToCurrency == to && x.IsActive).ToListAsync(ct);
        foreach (var x in old) x.IsActive = false;
        var item = new ExchangeRate
        {
            FromCurrency = from,
            ToCurrency = to,
            Rate = r.Rate,
            IsActive = r.IsActive,
            UpdatedByUserId = adminId
        };
        db.Add(item); await db.SaveChangesAsync(ct); return item;
    }
}
