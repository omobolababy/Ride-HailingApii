using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ride_HailingApi.DTOs.Ride;
using Ride_HailingApi.Enums;
using Ride_HailingApi.Helpers;
using Ride_HailingApi.Services.Interface;

namespace Ride_HailingApi.Controllers;

[ApiController]
[Route("api/ride")]
[Authorize]
public class RideController : ControllerBase
{
    private readonly IRideService _rideService;
    
        public RideController(IRideService rideService)
        {
            _rideService = rideService;
        }
    
        [HttpPost]
        [Authorize(Roles = nameof(UserRole.Passenger))]
        public async Task<IActionResult> CreateRide([FromBody] CreateRideRequest request)
        {
            var result = await _rideService.CreateRideAsync(User.GetUserId(), request);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    
        [HttpGet("my-rides")]
        [Authorize(Roles = nameof(UserRole.Passenger))]
        public async Task<IActionResult> GetMyRides()
        {
            var result = await _rideService.GetMyRidesAsync(User.GetUserId());
            return Ok(result);
        }
    
        [HttpGet("assigned-to-me")]
        [Authorize(Roles = nameof(UserRole.Driver))]
        public async Task<IActionResult> GetMyAssignedRides()
        {
            var result = await _rideService.GetRidesForDriverAsync(User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }
    
        // Resource-ownership is enforced inside the service: a Passenger may only view
        // their own ride, and a Driver only a ride assigned to them. Admins may view any.
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetRideById(int id)
        {
            var role = Enum.Parse<UserRole>(User.GetRole());
            var result = await _rideService.GetRideByIdAsync(id, User.GetUserId(), role);
            if (!result.Success)
            {
                return result.Message.Contains("permission", StringComparison.OrdinalIgnoreCase)
                    ? Forbid()
                    : NotFound(result);
            }
            return Ok(result);
        }
    
        [HttpPut("{id:int}/cancel")]
        [Authorize(Roles = nameof(UserRole.Passenger))]
        public async Task<IActionResult> CancelRide(int id, [FromBody] CancelRideRequest request)
        {
            var result = await _rideService.CancelRideAsync(id, User.GetUserId(), request);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    
        [HttpPut("{id:int}/accept")]
        [Authorize(Roles = nameof(UserRole.Driver))]
        public async Task<IActionResult> AcceptRide(int id)
        {
            var result = await _rideService.AcceptRideAsync(id, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }
    
        [HttpPut("{id:int}/status")]
        [Authorize(Roles = nameof(UserRole.Driver))]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] RideStatusUpdateRequest request)
        {
            var result = await _rideService.UpdateRideStatusAsync(id, User.GetUserId(), request);
            return result.Success ? Ok(result) : BadRequest(result);
        }
}