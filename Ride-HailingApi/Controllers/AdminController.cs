using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ride_HailingApi.DTOs.Admin;
using Ride_HailingApi.DTOs.Driver;
using Ride_HailingApi.Enums;
using Ride_HailingApi.Helpers;
using Ride_HailingApi.Services.Interface;

namespace Ride_HailingApi.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = nameof(UserRole.Admin))]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly IRideService _rideService;

    public AdminController(IAdminService adminService, IRideService rideService)
    {
        _adminService = adminService;
        _rideService = rideService;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] string? role)
    {
        var result = await _adminService.GetAllUsersAsync(role);
        return Ok(result);
    }

    [HttpPut("users/{id:int}/active-status")]
    public async Task<IActionResult> SetUserActiveStatus(int id, [FromBody] SetActiveStatusRequest request)
    {
        var result = await _adminService.SetUserActiveStatusAsync(id, request.IsActive, User.GetUserId());
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("drivers/pending")]
    public async Task<IActionResult> GetPendingDrivers()
    {
        var result = await _adminService.GetPendingDriversAsync();
        return Ok(result);
    }

    [HttpPut("drivers/{id:int}/approve")]
    public async Task<IActionResult> ApproveDriver(int id)
    {
        var result = await _adminService.ApproveDriverAsync(id, User.GetUserId());
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("drivers/{id:int}/reject")]
    public async Task<IActionResult> RejectDriver(int id, [FromBody] DriverRejectRequest request)
    {
        var result = await _adminService.RejectDriverAsync(id, request, User.GetUserId());
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("rides")]
    public async Task<IActionResult> GetAllRides()
    {
        var result = await _rideService.GetAllRidesAsync();
        return Ok(result);
    }

    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50)
    {
        var result = await _adminService.GetAuditLogsAsync(pageNumber, pageSize);
        return Ok(result);
    }
}
