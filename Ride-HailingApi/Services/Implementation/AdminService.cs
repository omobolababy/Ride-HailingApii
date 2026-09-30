using Microsoft.Extensions.Logging;
using Ride_HailingApi.DTOs.Admin;
using Ride_HailingApi.DTOs.Driver;
using Ride_HailingApi.Entities;
using Ride_HailingApi.Enums;
using Ride_HailingApi.Repositories.Interface;
using Ride_HailingApi.Services.Interface;
using Ride_HailingApi.Utils;
using RideHailingApi.DTOs.Common;

namespace Ride_HailingApi.Services.Implementation;

public class AdminService : IAdminService
{
    private readonly IUserRepository _userRepository;
    private readonly IDriverRepository _driverRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly IAuditService _auditService;
    private readonly ILogger<AdminService> _logger;

    public AdminService(IUserRepository userRepository, IDriverRepository driverRepository,
        IAuditLogRepository auditLogRepository, IEmailService emailService, ISmsService smsService,
        IAuditService auditService, ILogger<AdminService> logger)
    {
        _userRepository = userRepository;
        _driverRepository = driverRepository;
        _auditLogRepository = auditLogRepository;
        _emailService = emailService;
        _smsService = smsService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<UserResponse>>> GetAllUsersAsync(string? role)
    {
        var users = await _userRepository.GetAllWithFilterAsync(role);
        var response = users.Select(u => new UserResponse
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email,
            PhoneNumber = u.PhoneNumber,
            Role = u.Role.ToString(),
            IsActive = u.IsActive,
            IsEmailVerified = u.IsEmailVerified,
            IsPhoneVerified = u.IsPhoneVerified,
            CreatedAtUtc = u.CreatedAtUtc
        });
        return ApiResponse<IEnumerable<UserResponse>>.SuccessResponse(response);
    }

    public async Task<ApiResponse<string>> SetUserActiveStatusAsync(int userId, bool isActive, int adminUserId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
        {
            _logger.LogWarning("Admin {AdminUserId} tried to change status of unknown user {UserId}.", adminUserId, userId);
            return ApiResponse<string>.FailResponse("User not found.");
        }

        if (user.Role == UserRole.Admin)
        {
            _logger.LogWarning("Admin {AdminUserId} tried to change status of admin account {UserId}.", adminUserId, userId);
            return ApiResponse<string>.FailResponse("Admin accounts cannot be deactivated through this endpoint.");
        }

        user.IsActive = isActive;
        user.UpdatedAtUtc = DateTime.UtcNow;
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync();

        await _auditService.LogAsync(adminUserId, UserRole.Admin.ToString(),
            isActive ? "UserActivated" : "UserDeactivated", "User", user.Id.ToString());
        _logger.LogInformation("Admin {AdminUserId} {Action} user {UserId}.", adminUserId, isActive ? "activated" : "deactivated", user.Id);

        return ApiResponse<string>.SuccessResponse($"User account has been {(isActive ? "activated" : "deactivated")}.");
    }

    public async Task<ApiResponse<IEnumerable<DriverResponse>>> GetPendingDriversAsync()
    {
        var drivers = await _driverRepository.GetByApprovalStatusAsync(DriverApprovalStatus.Pending);
        return ApiResponse<IEnumerable<DriverResponse>>.SuccessResponse(drivers.Select(MapToResponse));
    }

    public async Task<ApiResponse<string>> ApproveDriverAsync(int driverProfileId, int adminUserId)
    {
        var driverProfile = await _driverRepository.GetByIdWithDetailsAsync(driverProfileId);
        if (driverProfile is null)
        {
            _logger.LogWarning("Admin {AdminUserId} tried to approve unknown driver profile {DriverProfileId}.", adminUserId, driverProfileId);
            return ApiResponse<string>.FailResponse("Driver profile not found.");
        }

        if (driverProfile.ApprovalStatus == DriverApprovalStatus.Approved)
        {
            _logger.LogWarning("Admin {AdminUserId} tried to approve driver profile {DriverProfileId}, which is already approved.", adminUserId, driverProfileId);
            return ApiResponse<string>.FailResponse("This driver is already approved.");
        }

        driverProfile.ApprovalStatus = DriverApprovalStatus.Approved;
        driverProfile.ApprovedByAdminId = adminUserId;
        driverProfile.ApprovedAtUtc = DateTime.UtcNow;
        driverProfile.RejectionReason = null;
        driverProfile.UpdatedAtUtc = DateTime.UtcNow;

        _driverRepository.Update(driverProfile);
        await _driverRepository.SaveChangesAsync();

        await _auditService.LogAsync(adminUserId, UserRole.Admin.ToString(), "DriverApproved", "DriverProfile",
            driverProfile.Id.ToString());
        _logger.LogInformation("Admin {AdminUserId} approved driver profile {DriverProfileId}.", adminUserId, driverProfile.Id);

        if (driverProfile.User is null)
        {
            _logger.LogError("User data missing for driver profile {DriverProfileId}.", driverProfileId);
            return ApiResponse<string>.FailResponse("Driver user data not found.");
        }

        await _emailService.SendDriverApplicationApprovedAsync(driverProfile.User.Email, driverProfile.User.FullName);
        await _smsService.SendSmsAsync(driverProfile.UserId, driverProfile.User.PhoneNumber,
            SmsUtils.DriverApplicationApproved());

        return ApiResponse<string>.SuccessResponse("Driver approved successfully.");
    }

    public async Task<ApiResponse<string>> RejectDriverAsync(int driverProfileId, DriverRejectRequest request, int adminUserId)
    {
        var driverProfile = await _driverRepository.GetByIdWithDetailsAsync(driverProfileId);
        if (driverProfile is null)
        {
            _logger.LogWarning("Admin {AdminUserId} tried to reject unknown driver profile {DriverProfileId}.", adminUserId, driverProfileId);
            return ApiResponse<string>.FailResponse("Driver profile not found.");
        }

        driverProfile.ApprovalStatus = DriverApprovalStatus.Rejected;
        driverProfile.RejectionReason = request.Reason;
        driverProfile.UpdatedAtUtc = DateTime.UtcNow;

        _driverRepository.Update(driverProfile);
        await _driverRepository.SaveChangesAsync();

        await _auditService.LogAsync(adminUserId, UserRole.Admin.ToString(), "DriverRejected", "DriverProfile",
            driverProfile.Id.ToString(), request.Reason);
        _logger.LogInformation("Admin {AdminUserId} rejected driver profile {DriverProfileId}.", adminUserId, driverProfile.Id);

        if (driverProfile.User is null)
        {
            _logger.LogError("User data missing for driver profile {DriverProfileId}.", driverProfileId);
            return ApiResponse<string>.FailResponse("Driver user data not found.");
        }

        await _emailService.SendDriverApplicationRejectedAsync(driverProfile.User.Email, driverProfile.User.FullName, request.Reason);
        await _smsService.SendSmsAsync(driverProfile.UserId, driverProfile.User.PhoneNumber,
            SmsUtils.DriverApplicationRejected(request.Reason));

        return ApiResponse<string>.SuccessResponse("Driver application rejected.");
    }

    public async Task<ApiResponse<PagedResult<AuditLogResponse>>> GetAuditLogsAsync(int pageNumber, int pageSize)
    {
        pageNumber = pageNumber < 1 ? 1 : pageNumber;
        pageSize = pageSize is < 1 or > 200 ? 50 : pageSize;

        var (items, totalCount) = await _auditLogRepository.GetPagedAsync(pageNumber, pageSize);

        var result = new PagedResult<AuditLogResponse>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items.Select(a => new AuditLogResponse
            {
                Id = a.Id,
                ActorUserId = a.ActorUserId,
                ActorRole = a.ActorRole,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Details = a.Details,
                WasSuccessful = a.WasSuccessful,
                CreatedAtUtc = a.CreatedAtUtc
            })
        };

        return ApiResponse<PagedResult<AuditLogResponse>>.SuccessResponse(result);
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
