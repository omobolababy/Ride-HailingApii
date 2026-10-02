using Microsoft.EntityFrameworkCore;
using Ride_HailingApi.DTOs.Driver;
using Ride_HailingApi.DTOs.Ride;
using Ride_HailingApi.Entities;
using Ride_HailingApi.Enums;
using Ride_HailingApi.Repositories.Interface;
using Ride_HailingApi.Services.Implementation;
using Ride_HailingApi.Helpers;
using Ride_HailingApi.Services.Interface;
using RideHailingApi.DTOs.Common;


namespace RideHailingApi.Services.Implementations;

public class DriverService : IDriverService
{
    private readonly IDriverRepository _driverRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRideRepository _rideRepository;
    private readonly IAuditService _auditService;
    private readonly ILogger<DriverService> _logger;

    public DriverService(IDriverRepository driverRepository, IUserRepository userRepository,
        IRideRepository rideRepository, IAuditService auditService, ILogger<DriverService> logger)
    {
        _driverRepository = driverRepository;
        _userRepository = userRepository;
        _rideRepository = rideRepository;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<ApiResponse<DriverResponse>> OnboardAsync(int userId, DriverOnboardRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null || user.Role != UserRole.Driver)
        {
            _logger.LogWarning("Onboarding rejected: user {UserId} is not a driver account.", userId);
            return ApiResponse<DriverResponse>.FailResponse("Only driver accounts can submit onboarding information.", ResponseCodes.Forbidden);
        }

        var existing = await _driverRepository.GetByUserIdAsync(userId);
        if (existing is not null)
        {
            _logger.LogWarning("Onboarding rejected: driver {UserId} has already submitted information.", userId);
            return ApiResponse<DriverResponse>.FailResponse("Driver information has already been submitted.", ResponseCodes.Conflict);
        }

        if (await _driverRepository.LicenseNumberExistsAsync(request.LicenseNumber))
        {
            _logger.LogWarning("Onboarding rejected for user {UserId}: license number already registered.", userId);
            return ApiResponse<DriverResponse>.FailResponse("This license number is already registered.", ResponseCodes.Conflict);
        }

        if (await _driverRepository.PlateNumberExistsAsync(request.PlateNumber))
        {
            _logger.LogWarning("Onboarding rejected for user {UserId}: plate number already registered.", userId);
            return ApiResponse<DriverResponse>.FailResponse("This plate number is already registered.", ResponseCodes.Conflict);
        }

        var driverProfile = new DriverProfile
        {
            UserId = userId,
            LicenseNumber = request.LicenseNumber,
            LicenseExpiryDate = request.LicenseExpiryDate,
            ApprovalStatus = DriverApprovalStatus.Pending,
            Vehicle = new Vehicle
            {
                Make = request.VehicleMake,
                Model = request.VehicleModel,
                Year = request.VehicleYear,
                Color = request.VehicleColor,
                PlateNumber = request.PlateNumber
            }
        };

        try
        {
            await _driverRepository.AddAsync(driverProfile);
            await _driverRepository.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            // Unique indexes on license/plate catch the race where two requests pass the exists-checks together.
            _logger.LogWarning(ex, "Onboarding failed on save for user {UserId}, most likely a duplicate license or plate number.", userId);
            return ApiResponse<DriverResponse>.FailResponse("This license number or plate number is already registered.", ResponseCodes.Conflict);
        }

        _logger.LogInformation("Driver {UserId} submitted onboarding information as profile {DriverProfileId}.", userId, driverProfile.Id);

        await _auditService.LogAsync(userId, user.Role.ToString(), "DriverOnboarded", "DriverProfile",
            driverProfile.Id.ToString(), "Driver submitted onboarding information; pending admin approval.");

        var full = await _driverRepository.GetByIdWithDetailsAsync(driverProfile.Id);
        return ApiResponse<DriverResponse>.SuccessResponse(MapToResponse(full!),
            "Driver information submitted successfully. Awaiting admin approval.", ResponseCodes.Created);
    }

    public async Task<ApiResponse<DriverResponse>> SetAvailabilityAsync(int userId, bool isAvailable)
    {
        var driverProfile = await _driverRepository.GetByUserIdAsync(userId);
        if (driverProfile is null)
        {
            return ApiResponse<DriverResponse>.FailResponse("Driver profile not found. Please complete onboarding first.", ResponseCodes.NotFound);
        }

        if (driverProfile.ApprovalStatus != DriverApprovalStatus.Approved)
        {
            _logger.LogWarning("Availability change rejected: driver {UserId} is not approved (status {Status}).", userId, driverProfile.ApprovalStatus);
            return ApiResponse<DriverResponse>.FailResponse("Your driver account has not been approved yet.", ResponseCodes.Forbidden);
        }

        driverProfile.IsAvailable = isAvailable;
        driverProfile.UpdatedAtUtc = DateTime.UtcNow;
        _driverRepository.Update(driverProfile);
        await _driverRepository.SaveChangesAsync();

        await _auditService.LogAsync(userId, UserRole.Driver.ToString(), "DriverAvailabilityChanged",
            "DriverProfile", driverProfile.Id.ToString(), $"Availability set to {isAvailable}");
        _logger.LogInformation("Driver {UserId} set availability to {IsAvailable}.", userId, isAvailable);

        return ApiResponse<DriverResponse>.SuccessResponse(MapToResponse(driverProfile),
            $"You are now marked as {(isAvailable ? "available" : "unavailable")}.");
    }

    public async Task<ApiResponse<IEnumerable<RideResponse>>> GetAvailableRideRequestsAsync(int userId)
    {
        var driverProfile = await _driverRepository.GetByUserIdAsync(userId);
        if (driverProfile is null)
        {
            return ApiResponse<IEnumerable<RideResponse>>.FailResponse("Driver profile not found.", ResponseCodes.NotFound);
        }

        if (driverProfile.ApprovalStatus != DriverApprovalStatus.Approved || !driverProfile.IsAvailable)
        {
            _logger.LogWarning("Ride requests denied: driver {UserId} is not approved and available.", userId);
            return ApiResponse<IEnumerable<RideResponse>>.FailResponse(
                "You must be an approved and available driver to view ride requests.", ResponseCodes.Forbidden);
        }

        var rides = await _rideRepository.GetAvailableRequestsAsync();
        return ApiResponse<IEnumerable<RideResponse>>.SuccessResponse(rides.Select(RideMapper.MapToResponse));
    }

    public async Task<ApiResponse<DriverResponse>> GetMyDriverProfileAsync(int userId)
    {
        var driverProfile = await _driverRepository.GetByUserIdAsync(userId);
        if (driverProfile is null)
        {
            return ApiResponse<DriverResponse>.FailResponse("Driver profile not found. Please complete onboarding first.", ResponseCodes.NotFound);
        }

        return ApiResponse<DriverResponse>.SuccessResponse(MapToResponse(driverProfile));
    }

    private static DriverResponse MapToResponse(DriverProfile d) => new()
    {
        DriverProfileId = d.Id,
        UserId = d.UserId,
        FullName = d.User.FullName,
        Email = d.User.Email,
        PhoneNumber = d.User.PhoneNumber,
        LicenseNumber = d.LicenseNumber,
        ApprovalStatus = d.ApprovalStatus.ToString(),
        IsAvailable = d.IsAvailable,
        CreatedAtUtc = d.CreatedAtUtc,
        Vehicle = d.Vehicle is null ? null : new VehicleResponse
        {
            Make = d.Vehicle.Make,
            Model = d.Vehicle.Model,
            Year = d.Vehicle.Year,
            Color = d.Vehicle.Color,
            PlateNumber = d.Vehicle.PlateNumber
        }
    };
}
