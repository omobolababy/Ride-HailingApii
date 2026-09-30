using Microsoft.EntityFrameworkCore;
using Ride_HailingApi.Entities;
using Ride_HailingApi.Enums;
using Ride_HailingApi.Repositories.Interface;
using RideHailingApi.Data;

namespace Ride_HailingApi.Repositories.Implementation;

public class DriverRepository : GenericRepository<DriverProfile>, IDriverRepository
{
    public DriverRepository(ApplicationDbContext context) : base(context) { }

    public async Task<DriverProfile?> GetByUserIdAsync(int userId) =>
        await DbSet.Include(d => d.Vehicle)
            .Include(d => d.User)
            .SingleOrDefaultAsync(d => d.UserId == userId);

    public async Task<DriverProfile?> GetByIdWithDetailsAsync(int driverProfileId) =>
        await DbSet.Include(d => d.Vehicle)
            .Include(d => d.User)
            .SingleOrDefaultAsync(d => d.Id == driverProfileId);

    public async Task<IEnumerable<DriverProfile>> GetByApprovalStatusAsync(DriverApprovalStatus status) =>
        await DbSet.Include(d => d.Vehicle)
            .Include(d => d.User)
            .Where(d => d.ApprovalStatus == status)
            .OrderBy(d => d.CreatedAtUtc)
            .ToListAsync();

    public async Task<bool> LicenseNumberExistsAsync(string licenseNumber) =>
        await DbSet.AnyAsync(d => d.LicenseNumber == licenseNumber);

    public async Task<bool> PlateNumberExistsAsync(string plateNumber) =>
        await Context.Vehicles.AnyAsync(v => v.PlateNumber == plateNumber);
}
