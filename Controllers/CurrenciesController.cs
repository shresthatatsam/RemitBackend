using EverestRemit.Api.DTOs;
using EverestRemit.Api.Entities;
using EverestRemit.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EverestRemit.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class CurrenciesController(
    ICurrencyService currencyService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetCurrencies(
        CancellationToken ct)
    {
        var currencies = await currencyService.GetActiveAsync(ct);

        return Ok(
            ApiResponse<IReadOnlyList<Currency>>.Ok(
                currencies,
                "Currencies retrieved successfully"));
    }
}