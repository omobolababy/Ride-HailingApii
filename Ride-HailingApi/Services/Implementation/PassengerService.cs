using Ride_HailingApi.DTOs.Passenger;
using Ride_HailingApi.Repositories.Interface;
using Ride_HailingApi.Helpers;
using Ride_HailingApi.Services.Interface;
using RideHailingApi.DTOs.Common;

namespace Ride_HailingApi.Services.Implementation;

public class PassengerService : IPassengerService
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditService _auditService;
    private readonly ILogger<PassengerService> _logger;

    public PassengerService(IUserRepository userRepository, IAuditService auditService, ILogger<PassengerService> logger)
    {
        _userRepository = userRepository;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<ApiResponse<PassengerProfileResponse>> GetProfileAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
        {
            _logger.LogWarning("Profile request failed: user {UserId} not found.", userId);
            return ApiResponse<PassengerProfileResponse>.FailResponse("User not found.", ResponseCodes.NotFound);
        }

        return ApiResponse<PassengerProfileResponse>.SuccessResponse(MapToResponse(user));
    }

    public async Task<ApiResponse<PassengerProfileResponse>> UpdateProfileAsync(int userId, UpdatePassengerProfileRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
        {
            _logger.LogWarning("Profile update failed: user {UserId} not found.", userId);
            return ApiResponse<PassengerProfileResponse>.FailResponse("User not found.", ResponseCodes.NotFound);
        }

        if (user.PhoneNumber != request.PhoneNumber && await _userRepository.PhoneNumberExistsAsync(request.PhoneNumber))
        {
            _logger.LogWarning("Profile update rejected for user {UserId}: phone number already in use.", userId);
            return ApiResponse<PassengerProfileResponse>.FailResponse("This phone number is already in use.", ResponseCodes.Conflict);
        }

        var phoneChanged = user.PhoneNumber != request.PhoneNumber;

        user.FullName = request.FullName;
        user.PhoneNumber = request.PhoneNumber;
        if (phoneChanged)
        {
            // Changing the phone number invalidates the previous verification.
            user.IsPhoneVerified = false;
        }
        user.UpdatedAtUtc = DateTime.UtcNow;

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync();

        await _auditService.LogAsync(user.Id, user.Role.ToString(), "ProfileUpdated", "User", user.Id.ToString());
        _logger.LogInformation("User {UserId} updated their profile (phone number changed: {PhoneChanged}).", user.Id, phoneChanged);

        return ApiResponse<PassengerProfileResponse>.SuccessResponse(MapToResponse(user), "Profile updated successfully.");
    }

    private static PassengerProfileResponse MapToResponse(Entities.User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        PhoneNumber = user.PhoneNumber,
        IsEmailVerified = user.IsEmailVerified,
        IsPhoneVerified = user.IsPhoneVerified,
        CreatedAtUtc = user.CreatedAtUtc
    };
}
