using AlDawahPharma.Application.Common;
using AlDawahPharma.Application.DTOs;
using AlDawahPharma.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlDawahPharma.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/expiry-alerts")]
[Authorize]
public class ExpiryAlertsController : ControllerBase
{
    private readonly IExpiryAlertRepository _expiryAlertRepository;

    public ExpiryAlertsController(IExpiryAlertRepository expiryAlertRepository)
    {
        _expiryAlertRepository = expiryAlertRepository;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<ExpiryAlertDto>>>> GetAll([FromQuery] string? status)
    {
        var list = await _expiryAlertRepository.GetAllAsync(status);
        return Ok(ApiResponse<IEnumerable<ExpiryAlertDto>>.Ok(list));
    }

    [HttpPut("{id}/notification")]
    [Authorize(Roles = "Admin,Pharmacist")]
    public async Task<ActionResult<ApiResponse>> UpdateNotificationStatus(long id, [FromBody] UpdateAlertStatusRequest request)
    {
        await _expiryAlertRepository.UpdateNotificationStatusAsync(id, request.NotificationSent);
        return Ok(ApiResponse.Ok("Notification status updated successfully."));
    }

    [HttpPost("refresh")]
    [Authorize(Roles = "Admin,Pharmacist")]
    public async Task<ActionResult<ApiResponse<int>>> RefreshAlerts()
    {
        var count = await _expiryAlertRepository.RefreshAlertsAsync();
        return Ok(ApiResponse<int>.Ok(count, $"Synchronized expiry alerts. {count} new alert(s) generated."));
    }
}
