using AlDawahPharma.Application.Common;
using AlDawahPharma.Application.DTOs;
using AlDawahPharma.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlDawahPharma.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportRepository _reportRepository;

    public ReportsController(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    [HttpGet("expired")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ExpiredMedicineReportDto>>>> GetExpired()
    {
        var data = await _reportRepository.GetExpiredMedicinesAsync();
        return Ok(ApiResponse<IEnumerable<ExpiredMedicineReportDto>>.Ok(data, "Expired medicines report loaded from ExpiredMedicine_View."));
    }

    [HttpGet("near-expiry")]
    public async Task<ActionResult<ApiResponse<IEnumerable<NearExpiryReportDto>>>> GetNearExpiry()
    {
        var data = await _reportRepository.GetNearExpiryMedicinesAsync();
        return Ok(ApiResponse<IEnumerable<NearExpiryReportDto>>.Ok(data, "Near-expiry medicines report loaded from NearExpiryMedicine_View."));
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<ApiResponse<IEnumerable<LowStockReportDto>>>> GetLowStock()
    {
        var data = await _reportRepository.GetLowStockMedicinesAsync();
        return Ok(ApiResponse<IEnumerable<LowStockReportDto>>.Ok(data, "Low stock medicines report loaded from LowStock_View."));
    }

    [HttpGet("monthly-sales")]
    public async Task<ActionResult<ApiResponse<IEnumerable<MonthlySalesReportDto>>>> GetMonthlySales()
    {
        var data = await _reportRepository.GetMonthlySalesAsync();
        return Ok(ApiResponse<IEnumerable<MonthlySalesReportDto>>.Ok(data, "Monthly sales report loaded from MonthlySales_View."));
    }

    [HttpGet("highest-selling")]
    public async Task<ActionResult<ApiResponse<IEnumerable<TopSellingMedicineDto>>>> GetTopSelling([FromQuery] int top = 10)
    {
        var data = await _reportRepository.GetTopSellingMedicinesAsync(top);
        return Ok(ApiResponse<IEnumerable<TopSellingMedicineDto>>.Ok(data, "Top selling medicines loaded successfully."));
    }

    [HttpGet("company-stock")]
    public async Task<ActionResult<ApiResponse<IEnumerable<CompanyStockReportDto>>>> GetCompanyStock()
    {
        var data = await _reportRepository.GetCompanyStockAsync();
        return Ok(ApiResponse<IEnumerable<CompanyStockReportDto>>.Ok(data, "Company stock analytics loaded from CompanyWiseStock_View."));
    }

    [HttpGet("supplier-purchase")]
    public async Task<ActionResult<ApiResponse<IEnumerable<SupplierPurchaseReportDto>>>> GetSupplierPurchase()
    {
        var data = await _reportRepository.GetSupplierPurchasesAsync();
        return Ok(ApiResponse<IEnumerable<SupplierPurchaseReportDto>>.Ok(data, "Supplier purchase analytics loaded from SupplierWisePurchase_View."));
    }

    [HttpGet("inventory-value")]
    public async Task<ActionResult<ApiResponse<IEnumerable<InventoryValuationReportDto>>>> GetInventoryValuation()
    {
        var data = await _reportRepository.GetInventoryValuationAsync();
        return Ok(ApiResponse<IEnumerable<InventoryValuationReportDto>>.Ok(data, "Inventory valuation report loaded from InventoryValue_View."));
    }
}
