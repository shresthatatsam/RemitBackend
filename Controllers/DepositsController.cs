
//using EverestRemit.Api.DTOs;
//using EverestRemit.Api.Interfaces;
//using EverestRemit.Api.Services;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Identity.Client.Extensions.Msal;
//using System.Security.Claims;

//namespace EverestRemit.Api.Controllers;

//[ApiController]
//[Route("api/deposits")]
//[Authorize]
//public sealed class DepositsController(
//    IDepositService depositService,
//    DepositFileStorage fileStorage) : ControllerBase
//{
//    private string? CurrentUserId =>
//        User.FindFirstValue(ClaimTypes.NameIdentifier)
//        ?? User.FindFirstValue("sub");

//    // POST: api/deposits
//    // User creates a deposit and uploads proof image.
//    [HttpPost]
//    [Consumes("multipart/form-data")]
//    [RequestSizeLimit(6 * 1024 * 1024)]
//    public async Task<IActionResult> Create(
//        [FromForm] CreateDepositRequest request,
//        CancellationToken ct)
//    {
//        var userId = CurrentUserId;

//        if (string.IsNullOrWhiteSpace(userId))
//            return Unauthorized();

//        try
//        {
//            var deposit = await depositService.CreateAsync(
//                userId, request, ct);

//            return StatusCode(
//                StatusCodes.Status201Created,
//                new
//                {
//                    deposit.Id,
//                    deposit.Amount,
//                    deposit.Currency,
//                    deposit.ReferenceNumber,
//                    deposit.Status,
//                    deposit.CreatedAtUtc
//                });
//        }
//        catch (ArgumentException ex)
//        {
//            return BadRequest(new { message = ex.Message });
//        }
//    }

//    // GET: api/deposits/my
//    // User sees only their own deposits.
//    [HttpGet("my")]
//    public async Task<IActionResult> GetMine(
//        CancellationToken ct)
//    {
//        var userId = CurrentUserId;

//        if (string.IsNullOrWhiteSpace(userId))
//            return Unauthorized();

//        var deposits = await depositService.GetMineAsync(
//            userId, ct);

//        return Ok(deposits);
//    }

//    // GET: api/deposits/admin/all
//    // Admin sees all deposits.
//    [HttpGet("admin/all")]
//    [Authorize(Roles = "Admin")]
//    public async Task<IActionResult> GetAll(
//        CancellationToken ct)
//    {
//        var deposits = await depositService.GetAllAsync(ct);

//        return Ok(deposits);
//    }

//    // PUT: api/deposits/admin/5/review
//    // Admin confirms or rejects a pending deposit.
//    [HttpPut("admin/{id:int}/review")]
//    [Authorize(Roles = "Admin")]
//    public async Task<IActionResult> Review(
//        int id,
//        [FromBody] ReviewDepositRequest request,
//        CancellationToken ct)
//    {
//        try
//        {
//            var deposit = await depositService.ReviewAsync(
//                id, request, ct);

//            if (deposit is null)
//            {
//                return NotFound(new
//                {
//                    message = "Deposit not found."
//                });
//            }

//            return Ok(new
//            {
//                deposit.Id,
//                deposit.UserId,
//                deposit.Amount,
//                deposit.Currency,
//                deposit.ReferenceNumber,
//                deposit.Status,
//                deposit.AdminNote,
//                deposit.CreatedAtUtc,
//                deposit.ReviewedAtUtc
//            });
//        }
//        catch (ArgumentException ex)
//        {
//            return BadRequest(new { message = ex.Message });
//        }
//        catch (InvalidOperationException ex)
//        {
//            return Conflict(new { message = ex.Message });
//        }
//    }

//    // GET: api/deposits/5/proof
//    // Only the deposit owner or an Admin can view the proof image.
//    [HttpGet("{id:int}/proof")]
//    public async Task<IActionResult> GetProof(
//        int id,
//        CancellationToken ct)
//    {
//        var userId = CurrentUserId;

//        if (string.IsNullOrWhiteSpace(userId))
//            return Unauthorized();

//        // GetByIdAsync must return the deposit without exposing
//        // the storage folder publicly.
//        var deposits = await depositService.GetMineAsync(
//            userId, ct);

//        var deposit = deposits.FirstOrDefault(x => x.Id == id);

//        if (User.IsInRole("Admin"))
//        {
//            var allDeposits = await depositService.GetAllAsync(ct);
//            deposit = allDeposits.FirstOrDefault(x => x.Id == id);
//        }

//        if (deposit is null)
//            return NotFound();

//        if (string.IsNullOrWhiteSpace(deposit.ProofUrl))
//            return NotFound(new { message = "Proof image not found." });

//        try
//        {
//            var path = fileStorage.GetPath(deposit.ProofUrl);

//            if (!System.IO.File.Exists(path))
//                return NotFound(new { message = "Proof image file not found." });

//            var extension = Path.GetExtension(path).ToLowerInvariant();

//            var contentType = extension switch
//            {
//                ".jpg" or ".jpeg" => "image/jpeg",
//                ".png" => "image/png",
//                ".webp" => "image/webp",
//                _ => "application/octet-stream"
//            };

//            return PhysicalFile(
//                path,
//                contentType,
//                enableRangeProcessing: false);
//        }
//        catch (ArgumentException)
//        {
//            return BadRequest(new { message = "Invalid proof reference." });
//        }
//    }


//    [HttpGet("{key}")]
//    public IActionResult ViewImage(string key)
//    {
//        try
//        {
//            var contentType = fileStorage.GetContentType(key);

//            if (contentType == null)
//                return NotFound();

//            var stream = fileStorage.OpenRead(key);

//            // Do not cache confidential deposit images.
//            Response.Headers["Cache-Control"] =
//                "private, no-store";

//            Response.Headers["X-Content-Type-Options"] =
//                "nosniff";

//            return File(
//                stream,
//                contentType,
//                enableRangeProcessing: false);
//        }
//        catch (FileNotFoundException)
//        {
//            return NotFound();
//        }
//        catch (ArgumentException)
//        {
//            return NotFound();
//        }
//        catch (DirectoryNotFoundException)
//        {
//            return NotFound();
//        }
//    }

//    // GET: /api/deposit-files/{key}/download
//    [HttpGet("{key}/download")]
//    public IActionResult Download(string key)
//    {
//        try
//        {
//            var contentType = fileStorage.GetContentType(key);

//            if (contentType == null)
//                return NotFound();

//            var stream = fileStorage.OpenRead(key);

//            Response.Headers["Cache-Control"] =
//                "private, no-store";

//            Response.Headers["X-Content-Type-Options"] =
//                "nosniff";

//            return File(
//                stream,
//                contentType,
//                fileDownloadName: "deposit-proof",
//                enableRangeProcessing: false);
//        }
//        catch (FileNotFoundException)
//        {
//            return NotFound();
//        }
//        catch (ArgumentException)
//        {
//            return NotFound();
//        }
//        catch (DirectoryNotFoundException)
//        {
//            return NotFound();
//        }
//    }

//}
