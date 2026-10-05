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
public class SalesController : ControllerBase
{
    private readonly ISaleRepository _saleRepository;

    public SalesController(ISaleRepository saleRepository)
    {
        _saleRepository = saleRepository;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<SaleDto>>>> GetAll()
    {
        var list = await _saleRepository.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<SaleDto>>.Ok(list));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<SaleDto>>> GetById(long id)
    {
        var item = await _saleRepository.GetByIdAsync(id);
        if (item == null) throw new NotFoundException("Sale", id);
        return Ok(ApiResponse<SaleDto>.Ok(item));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Pharmacist,Staff")]
    public async Task<ActionResult<ApiResponse<SaleDto>>> Create([FromBody] CreateSaleRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedException();

        if (request.CustomerID <= 0)
            throw new ValidationException("A valid customer must be selected.");
        if (request.Items == null || request.Items.Count == 0)
            throw new ValidationException("At least one medicine item is required for sale.");

        var created = await _saleRepository.CreateAsync(request, userId);
        return CreatedAtAction(nameof(GetById), new { id = created.SaleID },
            ApiResponse<SaleDto>.Ok(created, "Sale invoice processed successfully. Inventory verified and reduced via database trigger."));
    }
}
