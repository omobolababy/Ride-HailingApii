using Ride_HailingApi.Entities;
using Ride_HailingApi.Enums;

namespace Ride_HailingApi.Repositories.Interface;

public interface IDriverRepository : IGenericRepository<DriverProfile>
{
    Task<DriverProfile?> GetByUserIdAsync(int userId);
    Task<DriverProfile?> GetByIdWithDetailsAsync(int driverProfileId);
    Task<IEnumerable<DriverProfile>> GetByApprovalStatusAsync(DriverApprovalStatus status);
    Task<bool> LicenseNumberExistsAsync(string licenseNumber);
    Task<bool> PlateNumberExistsAsync(string plateNumber);
 
}