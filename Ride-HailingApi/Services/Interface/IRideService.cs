using Ride_HailingApi.DTOs.Ride;
using Ride_HailingApi.Enums;
using RideHailingApi.DTOs.Common;

namespace Ride_HailingApi.Services.Interface;

public interface IRideService
{
    Task<ApiResponse<RideResponse>> CreateRideAsync(int passengerId, CreateRideRequest request);
    Task<ApiResponse<IEnumerable<RideResponse>>> GetMyRidesAsync(int passengerId);
    Task<ApiResponse<RideResponse>> GetRideByIdAsync(int rideId, int requestingUserId, UserRole requestingUserRole);
    Task<ApiResponse<string>> CancelRideAsync(int rideId, int passengerId, CancelRideRequest request);
    Task<ApiResponse<RideResponse>> AcceptRideAsync(int rideId, int driverUserId);
    Task<ApiResponse<RideResponse>> UpdateRideStatusAsync(int rideId, int driverUserId, RideStatusUpdateRequest request);
    Task<ApiResponse<IEnumerable<RideResponse>>> GetRidesForDriverAsync(int driverUserId);
    Task<ApiResponse<IEnumerable<RideResponse>>> GetAllRidesAsync();
}