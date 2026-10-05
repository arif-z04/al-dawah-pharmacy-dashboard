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
public class StockController : ControllerBase
{
    private readonly IStockLogRepository _stockLogRepository;

    public StockController(IStockLogRepository stockLogRepository)
    {
        _stockLogRepository = stockLogRepository;
    }

    [HttpGet]
    [HttpGet("log")]
    [HttpGet("/api/stock-log")]
    public async Task<ActionResult<ApiResponse<IEnumerable<StockLogDto>>>> GetLogs(
        [FromQuery] long? medicineId,
        [FromQuery] string? actionType)
    {
        var list = await _stockLogRepository.GetAllAsync(medicineId, actionType);
        return Ok(ApiResponse<IEnumerable<StockLogDto>>.Ok(list));
    }

    [HttpPost("adjust")]
    [HttpPost("adjustment")]
    [HttpPost("/api/stock-log/adjust")]
    [Authorize(Roles = "Admin,Pharmacist")]
    public async Task<ActionResult<ApiResponse<StockLogDto>>> AdjustStock([FromBody] CreateStockAdjustmentRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedException();

        if (request.MedicineID <= 0)
            throw new ValidationException("Medicine must be selected.");
        if (request.Quantity == 0)
            throw new ValidationException("Adjustment quantity cannot be zero.");

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            request.Reason = !string.IsNullOrWhiteSpace(request.Remarks)
                ? request.Remarks
                : "Manual inventory adjustment";
        }

        var log = await _stockLogRepository.RecordAdjustmentAsync(request, userId);
        return Ok(ApiResponse<StockLogDto>.Ok(log, "Stock adjusted successfully and movement recorded in StockLog."));
    }
}
