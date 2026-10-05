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
public class MedicinesController : ControllerBase
{
    private readonly IMedicineRepository _medicineRepository;

    public MedicinesController(IMedicineRepository medicineRepository)
    {
        _medicineRepository = medicineRepository;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<MedicineDto>>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] long? categoryId,
        [FromQuery] long? companyId)
    {
        var list = await _medicineRepository.GetAllAsync(search, categoryId, companyId);
        return Ok(ApiResponse<IEnumerable<MedicineDto>>.Ok(list));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<MedicineDto>>> GetById(long id)
    {
        var item = await _medicineRepository.GetByIdAsync(id);
        if (item == null) throw new NotFoundException("Medicine", id);
        return Ok(ApiResponse<MedicineDto>.Ok(item));
    }

    [HttpGet("{id}/stock")]
    public async Task<ActionResult<ApiResponse<int>>> GetStock(long id)
    {
        var stock = await _medicineRepository.GetAvailableStockAsync(id);
        return Ok(ApiResponse<int>.Ok(stock, "Stock retrieved using GET_AVAILABLE_STOCK function."));
    }

    [HttpGet("{id}/inventory-value")]
    public async Task<ActionResult<ApiResponse<decimal>>> GetInventoryValue(long id)
    {
        var value = await _medicineRepository.GetInventoryValueAsync(id);
        return Ok(ApiResponse<decimal>.Ok(value, "Inventory value retrieved using GET_INVENTORY_VALUE function."));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Pharmacist")]
    public async Task<ActionResult<ApiResponse<MedicineDto>>> Create([FromBody] CreateMedicineRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.MedicineName))
            throw new ValidationException("Medicine name is required.");
        if (string.IsNullOrWhiteSpace(request.BatchNumber))
            throw new ValidationException("Batch number is required.");
        if (request.SellingPrice < request.PurchasePrice)
            throw new ValidationException("Selling price cannot be less than purchase price.");
        if (request.ExpiryDate <= request.ManufacturingDate)
            throw new ValidationException("Expiry date must be after manufacturing date.");

        var created = await _medicineRepository.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.MedicineID }, ApiResponse<MedicineDto>.Ok(created, "Medicine registered successfully via ADD_MEDICINE procedure."));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Pharmacist")]
    public async Task<ActionResult<ApiResponse>> Update(long id, [FromBody] UpdateMedicineRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.MedicineName))
            throw new ValidationException("Medicine name is required.");
        if (string.IsNullOrWhiteSpace(request.BatchNumber))
            throw new ValidationException("Batch number is required.");

        var existing = await _medicineRepository.GetByIdAsync(id);
        if (existing == null) throw new NotFoundException("Medicine", id);

        await _medicineRepository.UpdateAsync(id, request);
        return Ok(ApiResponse.Ok("Medicine updated successfully."));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse>> Delete(long id)
    {
        var existing = await _medicineRepository.GetByIdAsync(id);
        if (existing == null) throw new NotFoundException("Medicine", id);

        await _medicineRepository.DeleteAsync(id);
        return Ok(ApiResponse.Ok("Medicine deleted successfully."));
    }
}
