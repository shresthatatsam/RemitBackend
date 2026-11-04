using EverestRemit.Api.DTOs;
using EverestRemit.Api.Entities;
using EverestRemit.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EverestRemit.Api.Controllers;

[ApiController]
[Route("api/beneficiaries")]
[Authorize(Roles = "User")]
public sealed class BeneficiariesController(IBeneficiaryService s)
    : ControllerBase
{
    private string U =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;


    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await s.GetMineAsync(U, ct);

        return Ok(ApiResponse<IReadOnlyList<Beneficiary>>.Ok(
            result,
            "Beneficiaries retrieved successfully"));
    }

    // GET: api/beneficiaries/mine?pageNumber=1&pageSize=10
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await s.GetMineAsync(
            U,
            pageNumber,
            pageSize,
            ct);

        return Ok(ApiResponse<PaginatedResponse<Beneficiary>>.Ok(
            result,
            "Beneficiaries retrieved successfully"));
    }

    // POST: api/beneficiaries
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] BeneficiaryRequest r,
        CancellationToken ct)
    {
        var result = await s.CreateAsync(U, r, ct);

        return CreatedAtAction(
            nameof(GetMine),
            new { pageNumber = 1, pageSize = 10 },
            ApiResponse<Beneficiary>.Ok(
                result,
                "Beneficiary created successfully"));
    }

    // PUT: api/beneficiaries/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] BeneficiaryRequest r,
        CancellationToken ct)
    {
        var result = await s.UpdateAsync(U, id, r, ct);

        if (result == null)
        {
            return NotFound(
                ApiResponse<object>.Fail(
                    "Beneficiary not found"));
        }

        return Ok(ApiResponse<Beneficiary>.Ok(
            result,
            "Beneficiary updated successfully"));
    }

    // DELETE: api/beneficiaries/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken ct)
    {
        try
        {
            var deleted = await s.DeleteAsync(U, id, ct);

            if (!deleted)
            {
                return NotFound(
                    ApiResponse<object>.Fail(
                        "Beneficiary not found"));
            }

            return Ok(ApiResponse<object>.Ok(
                null!,
                "Beneficiary deleted successfully"));
        }
        catch (InvalidOperationException e)
        {
            return Conflict(
                ApiResponse<object>.Fail(e.Message));
        }
    }
}