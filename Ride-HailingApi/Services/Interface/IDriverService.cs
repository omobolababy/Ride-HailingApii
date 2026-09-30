using Ride_HailingApi.DTOs.Driver;
using Ride_HailingApi.DTOs.Ride;
using RideHailingApi.DTOs.Common;

namespace Ride_HailingApi.Services.Interface;

public interface IDriverService
{
    Task<ApiResponse<DriverResponse>> OnboardAsync(int userId, DriverOnboardRequest request);
    Task<ApiResponse<DriverResponse>> SetAvailabilityAsync(int userId, bool isAvailable);
    Task<ApiResponse<IEnumerable<RideResponse>>> GetAvailableRideRequestsAsync(int userId);
    Task<ApiResponse<DriverResponse>> GetMyDriverProfileAsync(int userId);

}