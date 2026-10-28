
using EverestRemit.Api.DTOs;
using EverestRemit.Api.Interfaces;
using EverestRemit.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EverestRemit.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class TransfersController(
    ITransferService transferService,
    DepositFileStorage fileStorage) : ControllerBase
{

    private string? CurrentUserId =>
       User.FindFirstValue(ClaimTypes.NameIdentifier)
       ?? User.FindFirstValue("sub");

    // GET: api/transfers/mine
    // Customer: Get own transfers
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(
        CancellationToken ct)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var transfers = await transferService.GetMineAsync(
            userId, ct);

        return Ok(transfers);
    }

    // POST: api/transfers
    // Customer: Create transfer with payment proof
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create(
        [FromForm] CreateTransferRequest request,
        CancellationToken ct)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        try
        {
            var transfer = await transferService.CreateAsync(
                userId, request, ct);

            return CreatedAtAction(
                nameof(GetMine),
                new { },
                transfer);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // GET: api/transfers/admin
    // Admin: Get all transfers
    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll(
        CancellationToken ct)
    {
        var transfers = await transferService.GetAllAsync(ct);

        return Ok(transfers);
    }

    // PUT: api/transfers/admin/{id}/status
    // Admin: Update transfer status
    [HttpPut("admin/{id:int}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromBody] UpdateTransferStatusRequest request,
        CancellationToken ct)
    {
        try
        {
            var transfer = await transferService.UpdateStatusAsync(
                id, request, ct);

            if (transfer is null)
                return NotFound(new
                {
                    message = "Transfer not found."
                });

            return Ok(transfer);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}/proof")]
    public async Task<IActionResult> GetProof(
       int id,
       CancellationToken ct)
    {
        var userId = CurrentUserId;

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        // GetByIdAsync must return the deposit without exposing
        // the storage folder publicly.
        var deposits = await transferService.GetMineAsync(
            userId, ct);

        var deposit = deposits.FirstOrDefault(x => x.Id == id);

        if (User.IsInRole("Admin"))
        {
            var allDeposits = await transferService.GetAllAsync(ct);
            deposit = allDeposits.FirstOrDefault(x => x.Id == id);
        }

        if (deposit is null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(deposit.ProofUrl))
            return NotFound(new { message = "Proof image not found." });

        try
        {
            var path = fileStorage.GetPath(deposit.ProofUrl);

            if (!System.IO.File.Exists(path))
                return NotFound(new { message = "Proof image file not found." });

            var extension = Path.GetExtension(path).ToLowerInvariant();

            var contentType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            return PhysicalFile(
                path,
                contentType,
                enableRangeProcessing: false);
        }
        catch (ArgumentException)
        {
            return BadRequest(new { message = "Invalid proof reference." });
        }
    }


    [HttpGet("{key}")]
    public IActionResult ViewImage(string key)
    {
        try
        {
            var contentType = fileStorage.GetContentType(key);

            if (contentType == null)
                return NotFound();

            var stream = fileStorage.OpenRead(key);

            // Do not cache confidential deposit images.
            Response.Headers["Cache-Control"] =
                "private, no-store";

            Response.Headers["X-Content-Type-Options"] =
                "nosniff";

            return File(
                stream,
                contentType,
                enableRangeProcessing: false);
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
        catch (DirectoryNotFoundException)
        {
            return NotFound();
        }
    }

    // GET: /api/deposit-files/{key}/download
    [HttpGet("{key}/download")]
    public IActionResult Download(string key)
    {
        try
        {
            var contentType = fileStorage.GetContentType(key);

            if (contentType == null)
                return NotFound();

            var stream = fileStorage.OpenRead(key);

            Response.Headers["Cache-Control"] =
                "private, no-store";

            Response.Headers["X-Content-Type-Options"] =
                "nosniff";

            return File(
                stream,
                contentType,
                fileDownloadName: "deposit-proof",
                enableRangeProcessing: false);
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
        catch (DirectoryNotFoundException)
        {
            return NotFound();
        }
    }


}
