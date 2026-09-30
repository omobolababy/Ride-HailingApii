using Ride_HailingApi.Entities;

namespace Ride_HailingApi.Repositories.Interface;

public interface IRideRepository : IGenericRepository<Ride>
{
    Task<Ride?> GetByReferenceAsync(string rideReference);
    Task<Ride?> GetByIdWithDetailsAsync(int id);
    Task<IEnumerable<Ride>> GetByPassengerIdAsync(int passengerId);
    Task<IEnumerable<Ride>> GetByDriverProfileIdAsync(int driverProfileId);
    Task<IEnumerable<Ride>> GetAvailableRequestsAsync();
    Task<Ride?> GetActiveRideForPassengerAsync(int passengerId);
    Task<bool> ReferenceExistsAsync(string rideReference);
}