using EverestRemit.Api.DTOs;
using EverestRemit.Api.Entities;
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
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var userId = CurrentUserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(
                ApiResponse<object>.Fail("Unauthorized"));
        }

        var transfers = await transferService.GetMineAsync(
            userId, ct);

        return Ok(
            ApiResponse<IReadOnlyList<RemittanceTransfer>>.Ok(
                transfers,
                "Transfers retrieved successfully"));
    }




    // POST: api/transfers
    // Customer: Create transfer with payment proof
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create(
        [FromForm] CreateTransferRequest request,
        CancellationToken ct)
    {
        var userId = CurrentUserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(
                ApiResponse<object>.Fail("Unauthorized"));
        }

        try
        {
            var transfer = await transferService.CreateAsync(
                userId, request, ct);

            return StatusCode(
                StatusCodes.Status201Created,
                ApiResponse<RemittanceTransfer>.Ok(
                    transfer,
                    "Transfer created successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(
                ApiResponse<object>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(
                ApiResponse<object>.Fail(ex.Message));
        }
    }

   
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
            {
                return NotFound(
                    ApiResponse<object>.Fail(
                        "Transfer not found."));
            }

            return Ok(
                ApiResponse<RemittanceTransfer>.Ok(
                    transfer,
                    "Transfer status updated successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(
                ApiResponse<object>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(
                ApiResponse<object>.Fail(ex.Message));
        }
    }

    // GET: api/transfers/{id}/proof
    // Customer: Own proof / Admin: Any proof
    [HttpGet("{id:int}/proof")]
    public async Task<IActionResult> GetProof(int id,CancellationToken ct)
    {
        var userId = CurrentUserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(
                ApiResponse<object>.Fail("Unauthorized"));
        }

        var deposit = await transferService.GetByIdAsync(
            id,
            userId,
            User.IsInRole("Admin"),
            ct);

        if (deposit is null)
        {
            return NotFound(
                ApiResponse<object>.Fail("Transfer not found."));
        }

        if (string.IsNullOrWhiteSpace(deposit.ProofUrl))
        {
            return NotFound(
                ApiResponse<object>.Fail("Proof image not found."));
        }

        try
        {
            var path = fileStorage.GetPath(deposit.ProofUrl);

            if (!System.IO.File.Exists(path))
            {
                return NotFound(
                    ApiResponse<object>.Fail(
                        "Proof image file not found."));
            }

            var extension = Path.GetExtension(path).ToLowerInvariant();

            var contentType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            Response.Headers["Cache-Control"] = "private, no-store";
            Response.Headers["X-Content-Type-Options"] = "nosniff";

            return PhysicalFile(
                path,
                contentType,
                enableRangeProcessing: false);
        }
        catch (ArgumentException)
        {
            return BadRequest(
                ApiResponse<object>.Fail("Invalid proof reference."));
        }
    }

    // GET: api/transfers/{key}
    // View uploaded image
    [HttpGet("{key}")]
    public IActionResult ViewImage(string key)
    {
        try
        {
            var contentType = fileStorage.GetContentType(key);

            if (contentType == null)
            {
                return NotFound(
                    ApiResponse<object>.Fail("Image not found."));
            }

            var stream = fileStorage.OpenRead(key);

            Response.Headers["Cache-Control"] = "private, no-store";
            Response.Headers["X-Content-Type-Options"] = "nosniff";

            return File(
                stream,
                contentType,
                enableRangeProcessing: false);
        }
        catch (FileNotFoundException)
        {
            return NotFound(
                ApiResponse<object>.Fail("Image file not found."));
        }
        catch (ArgumentException)
        {
            return NotFound(
                ApiResponse<object>.Fail("Invalid image reference."));
        }
        catch (DirectoryNotFoundException)
        {
            return NotFound(
                ApiResponse<object>.Fail("Image directory not found."));
        }
    }

    // GET: api/transfers/{key}/download
    // Download uploaded image
    [HttpGet("{key}/download")]
    public IActionResult Download(string key)
    {
        try
        {
            var contentType = fileStorage.GetContentType(key);

            if (contentType == null)
            {
                return NotFound(
                    ApiResponse<object>.Fail("File not found."));
            }

            var stream = fileStorage.OpenRead(key);

            Response.Headers["Cache-Control"] = "private, no-store";
            Response.Headers["X-Content-Type-Options"] = "nosniff";

            return File(
                stream,
                contentType,
                fileDownloadName: "deposit-proof",
                enableRangeProcessing: false);
        }
        catch (FileNotFoundException)
        {
            return NotFound(
                ApiResponse<object>.Fail("File not found."));
        }
        catch (ArgumentException)
        {
            return NotFound(
                ApiResponse<object>.Fail("Invalid file reference."));
        }
        catch (DirectoryNotFoundException)
        {
            return NotFound(
                ApiResponse<object>.Fail("File directory not found."));
        }
    }


    [HttpGet("mine/Pagination")]
    public async Task<IActionResult> GetMine([FromQuery] int pageNumber = 1,[FromQuery] int pageSize = 10,CancellationToken ct = default)
    {
        var userId = CurrentUserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(
                ApiResponse<object>.Fail("Unauthorized"));
        }

        var result = await transferService.GetMineAsync(
            userId,
            pageNumber,
            pageSize,
            ct);

        return Ok(
            ApiResponse<PaginatedResponse<TransferResponse>>.Ok(
                result,
                "Transfers retrieved successfully"));
    }

    [HttpGet("admin/Pagination")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1,[FromQuery] int pageSize = 10,CancellationToken ct = default)
    {
        var result = await transferService.GetAllAsync(
            pageNumber,
            pageSize,
            ct);

        return Ok(
            ApiResponse<PaginatedResponse<TransferResponse>>.Ok(
                result,
                "All transfers retrieved successfully"));
    }

}
