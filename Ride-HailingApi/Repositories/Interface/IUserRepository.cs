using Ride_HailingApi.Entities;

namespace Ride_HailingApi.Repositories.Interface;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByPhoneNumberAsync(string phoneNumber);
    Task<bool> EmailExistsAsync(string email);
    Task<bool> PhoneNumberExistsAsync(string phoneNumber);
    Task<IEnumerable<User>> GetAllWithFilterAsync(string? role);
}
