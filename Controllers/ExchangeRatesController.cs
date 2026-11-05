using EverestRemit.Api.DTOs;
using EverestRemit.Api.Entities;
using EverestRemit.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EverestRemit.Api.Controllers;

[ApiController]
[Route("api/exchange-rates")]
public sealed class ExchangeRatesController(
    IExchangeRateService exchangeRateService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var exchangeRates = await exchangeRateService.GetActiveAsync(ct);

        return Ok(
            ApiResponse<IReadOnlyList<ExchangeRate>>.Ok(
                exchangeRates,
                "Exchange rates retrieved successfully"));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(
        ExchangeRateRequest request,
        CancellationToken ct)
    {
        try
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(
                    ApiResponse<object>.Fail("Unauthorized"));
            }

            var exchangeRate = await exchangeRateService.CreateAsync(
                userId,
                request,
                ct);

            return Ok(
                ApiResponse<ExchangeRate>.Ok(
                    exchangeRate,
                    "Exchange rate created successfully"));
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(
                ApiResponse<object>.Fail(e.Message));
        }
    }
}