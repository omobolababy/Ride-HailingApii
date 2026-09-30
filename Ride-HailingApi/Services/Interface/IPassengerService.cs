using Ride_HailingApi.DTOs.Passenger;
using RideHailingApi.DTOs.Common;

namespace Ride_HailingApi.Services.Interface;

public interface IPassengerService
{
    Task<ApiResponse<PassengerProfileResponse>> GetProfileAsync(int userId);
    Task<ApiResponse<PassengerProfileResponse>> UpdateProfileAsync(int userId, UpdatePassengerProfileRequest request);
}