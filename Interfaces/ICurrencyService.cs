
using EverestRemit.Api.Entities;

namespace EverestRemit.Api.Interfaces;

public interface ICurrencyService
{
    Task<IReadOnlyList<Currency>> GetActiveAsync(
        CancellationToken ct);
}
