
using EverestRemit.Api.Data;
using EverestRemit.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace EverestRemit.Api.Data.Seeders;

public static class CurrencySeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext db,
        CancellationToken ct = default)
    {
        var currencies = new[]
        {
            new Currency
            {
                Code = "AUD",
                Name = "Australian Dollar",
                Symbol = "$",
                IsActive = true
            },
            new Currency
            {
                Code = "NPR",
                Name = "Nepalese Rupee",
                Symbol = "Rs",
                IsActive = true
            }
        };

        var existingCodes = await db.Currencies
            .Select(x => x.Code)
            .ToListAsync(ct);

        foreach (var currency in currencies)
        {
            if (!existingCodes.Contains(
                currency.Code,
                StringComparer.OrdinalIgnoreCase))
            {
                db.Currencies.Add(currency);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
