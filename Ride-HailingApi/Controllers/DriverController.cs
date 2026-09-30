using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ride_HailingApi.DTOs.Driver;
using Ride_HailingApi.Enums;
using Ride_HailingApi.Helpers;
using Ride_HailingApi.Services.Interface;

namespace Ride_HailingApi.Controllers;


[ApiController]
[Route("api/driver")]
[Authorize(Roles = nameof(UserRole.Driver))]
public class DriverController : ControllerBase
{
    private readonly IDriverService _driverService;

    public DriverController(IDriverService driverService)
    {
        _driverService = driverService;
    }

    [HttpPost("onboard")]
    public async Task<IActionResult> Onboard([FromBody] DriverOnboardRequest request)
    {
        var result = await _driverService.OnboardAsync(User.GetUserId(), request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var result = await _driverService.GetMyDriverProfileAsync(User.GetUserId());
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPut("availability")]
    public async Task<IActionResult> SetAvailability([FromBody] DriverAvailabilityRequest request)
    {
        var result = await _driverService.SetAvailabilityAsync(User.GetUserId(), request.IsAvailable);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("rides/available")]
    public async Task<IActionResult> GetAvailableRides()
    {
        var result = await _driverService.GetAvailableRideRequestsAsync(User.GetUserId());
        return result.Success ? Ok(result) : BadRequest(result);
    }
}