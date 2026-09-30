using Ride_HailingApi.DTOs.Admin;
using Ride_HailingApi.DTOs.Driver;
using RideHailingApi.DTOs.Common;

namespace Ride_HailingApi.Services.Interface;

public interface IAdminService
{
    Task<ApiResponse<IEnumerable<UserResponse>>> GetAllUsersAsync(string? role);
    Task<ApiResponse<string>> SetUserActiveStatusAsync(int userId, bool isActive, int adminUserId);
    Task<ApiResponse<IEnumerable<DriverResponse>>> GetPendingDriversAsync();
    Task<ApiResponse<string>> ApproveDriverAsync(int driverProfileId, int adminUserId);
    Task<ApiResponse<string>> RejectDriverAsync(int driverProfileId, DriverRejectRequest request, int adminUserId);
    Task<ApiResponse<PagedResult<AuditLogResponse>>> GetAuditLogsAsync(int pageNumber, int pageSize);

}