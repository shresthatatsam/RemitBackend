
using EverestRemit.Api.Data;
using EverestRemit.Api.Entities;
using EverestRemit.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EverestRemit.Api.Services;

public sealed class CurrencyService(
    ApplicationDbContext db) : ICurrencyService
{
    public async Task<IReadOnlyList<Currency>> GetActiveAsync(
        CancellationToken ct)
    {
        return await db.Currencies
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Code)
            .ToListAsync(ct);
    }
}
