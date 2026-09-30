using Microsoft.EntityFrameworkCore;
using Ride_HailingApi.Entities;
using Ride_HailingApi.Enums;
using Ride_HailingApi.Repositories.Interface;
using RideHailingApi.Data;

namespace Ride_HailingApi.Repositories.Implementation;

public class RideRepository : GenericRepository<Ride>, IRideRepository
{
    public RideRepository(ApplicationDbContext context) : base(context) { }

    private IQueryable<Ride> WithDetails() =>
        DbSet.Include(r => r.Passenger)
             .Include(r => r.DriverProfile).ThenInclude(d => d!.User);

    public async Task<Ride?> GetByReferenceAsync(string rideReference) =>
        await WithDetails().SingleOrDefaultAsync(r => r.RideReference == rideReference);

    public async Task<Ride?> GetByIdWithDetailsAsync(int id) =>
        await WithDetails().SingleOrDefaultAsync(r => r.Id == id);

    public async Task<IEnumerable<Ride>> GetByPassengerIdAsync(int passengerId) =>
        await WithDetails().Where(r => r.PassengerId == passengerId)
                            .OrderByDescending(r => r.RequestedAtUtc)
                            .ToListAsync();

    public async Task<IEnumerable<Ride>> GetByDriverProfileIdAsync(int driverProfileId) =>
        await WithDetails().Where(r => r.DriverProfileId == driverProfileId)
                            .OrderByDescending(r => r.RequestedAtUtc)
                            .ToListAsync();

    public async Task<IEnumerable<Ride>> GetAvailableRequestsAsync() =>
        await WithDetails().Where(r => r.Status == RideStatus.Requested && r.DriverProfileId == null)
                            .OrderBy(r => r.RequestedAtUtc)
                            .ToListAsync();

    public async Task<Ride?> GetActiveRideForPassengerAsync(int passengerId) =>
        await WithDetails().Where(r => r.PassengerId == passengerId &&
                                        r.Status != RideStatus.Completed &&
                                        r.Status != RideStatus.Cancelled)
                            .OrderByDescending(r => r.RequestedAtUtc)
                            .FirstOrDefaultAsync();

    public async Task<bool> ReferenceExistsAsync(string rideReference) =>
        await DbSet.AnyAsync(r => r.RideReference == rideReference);
}
