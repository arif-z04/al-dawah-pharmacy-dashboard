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
public class SuppliersController : ControllerBase
{
    private readonly ISupplierRepository _supplierRepository;

    public SuppliersController(ISupplierRepository supplierRepository)
    {
        _supplierRepository = supplierRepository;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<SupplierDto>>>> GetAll()
    {
        var list = await _supplierRepository.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<SupplierDto>>.Ok(list));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<SupplierDto>>> GetById(long id)
    {
        var item = await _supplierRepository.GetByIdAsync(id);
        if (item == null) throw new NotFoundException("Supplier", id);
        return Ok(ApiResponse<SupplierDto>.Ok(item));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Pharmacist")]
    public async Task<ActionResult<ApiResponse<SupplierDto>>> Create([FromBody] CreateSupplierRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SupplierName))
            throw new ValidationException("Supplier name is required.");

        var created = await _supplierRepository.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.SupplierID }, ApiResponse<SupplierDto>.Ok(created, "Supplier created successfully."));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Pharmacist")]
    public async Task<ActionResult<ApiResponse>> Update(long id, [FromBody] UpdateSupplierRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SupplierName))
            throw new ValidationException("Supplier name is required.");

        var existing = await _supplierRepository.GetByIdAsync(id);
        if (existing == null) throw new NotFoundException("Supplier", id);

        await _supplierRepository.UpdateAsync(id, request);
        return Ok(ApiResponse.Ok("Supplier updated successfully."));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> Delete(long id)
    {
        var existing = await _supplierRepository.GetByIdAsync(id);
        if (existing == null) throw new NotFoundException("Supplier", id);

        await _supplierRepository.DeleteAsync(id);
        return Ok(ApiResponse.Ok("Supplier deleted successfully."));
    }
}
