using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ride_HailingApi.DTOs.Passenger;
using Ride_HailingApi.Enums;
using Ride_HailingApi.Helpers;
using Ride_HailingApi.Services.Interface;

namespace Ride_HailingApi.Controllers;


[ApiController]
[Route("api/passenger")]
[Authorize(Roles = nameof(UserRole.Passenger))]
public class PassengerController : ControllerBase
{
    private readonly IPassengerService _passengerService;

    public PassengerController(IPassengerService passengerService)
    {
        _passengerService = passengerService;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var result = await _passengerService.GetProfileAsync(User.GetUserId());
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdatePassengerProfileRequest request)
    {
        var result = await _passengerService.UpdateProfileAsync(User.GetUserId(), request);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}