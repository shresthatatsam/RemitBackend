using System.Security.Claims;using EverestRemit.Api.DTOs;using EverestRemit.Api.Interfaces;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;
namespace EverestRemit.Api.Controllers;
[ApiController,Route("api/beneficiaries"),Authorize(Roles="User")]
public sealed class BeneficiariesController(IBeneficiaryService s):ControllerBase{string U=>User.FindFirstValue(ClaimTypes.NameIdentifier)!;
[HttpGet]public async Task<IActionResult> Get(CancellationToken ct)=>Ok(await s.GetMineAsync(U,ct));
[HttpPost]public async Task<IActionResult> Create(BeneficiaryRequest r,CancellationToken ct)=>Ok(await s.CreateAsync(U,r,ct));
[HttpPut("{id:int}")]public async Task<IActionResult> Update(int id,BeneficiaryRequest r,CancellationToken ct){var x=await s.UpdateAsync(U,id,r,ct);return x==null?NotFound():Ok(x);}
[HttpDelete("{id:int}")]public async Task<IActionResult> Delete(int id,CancellationToken ct){try{return await s.DeleteAsync(U,id,ct)?NoContent():NotFound();}catch(InvalidOperationException e){return Conflict(new{message=e.Message});}}}
