using System.Security.Claims;
using AlDawahPharma.Application.Common;
using AlDawahPharma.Application.DTOs;
using AlDawahPharma.Application.Exceptions;
using AlDawahPharma.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlDawahPharma.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PurchasesController : ControllerBase
{
    private readonly IPurchaseRepository _purchaseRepository;

    public PurchasesController(IPurchaseRepository purchaseRepository)
    {
        _purchaseRepository = purchaseRepository;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<PurchaseDto>>>> GetAll()
    {
        var list = await _purchaseRepository.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<PurchaseDto>>.Ok(list));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<PurchaseDto>>> GetById(long id)
    {
        var item = await _purchaseRepository.GetByIdAsync(id);
        if (item == null) throw new NotFoundException("Purchase", id);
        return Ok(ApiResponse<PurchaseDto>.Ok(item));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Pharmacist,Staff")]
    public async Task<ActionResult<ApiResponse<PurchaseDto>>> Create([FromBody] CreatePurchaseRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedException();

        if (request.SupplierID <= 0)
            throw new ValidationException("A valid supplier must be selected.");
        if (request.Items == null || request.Items.Count == 0)
            throw new ValidationException("At least one medicine item is required for purchase.");

        var created = await _purchaseRepository.CreateAsync(request, userId);
        return CreatedAtAction(nameof(GetById), new { id = created.PurchaseID },
            ApiResponse<PurchaseDto>.Ok(created, "Purchase recorded successfully. Stock and invoice totals updated automatically via database triggers."));
    }
}
