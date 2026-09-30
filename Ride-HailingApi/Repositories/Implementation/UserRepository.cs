using Microsoft.EntityFrameworkCore;
using Ride_HailingApi.Entities;
using Ride_HailingApi.Repositories.Interface;
using RideHailingApi.Data;

namespace Ride_HailingApi.Repositories.Implementation;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email) =>
        await DbSet.Include(u => u.DriverProfile)
            .SingleOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

    public async Task<User?> GetByPhoneNumberAsync(string phoneNumber) =>
        await DbSet.SingleOrDefaultAsync(u => u.PhoneNumber == phoneNumber);

    public async Task<bool> EmailExistsAsync(string email) =>
        await DbSet.AnyAsync(u => u.Email.ToLower() == email.ToLower());

    public async Task<bool> PhoneNumberExistsAsync(string phoneNumber) =>
        await DbSet.AnyAsync(u => u.PhoneNumber == phoneNumber);

    public async Task<IEnumerable<User>> GetAllWithFilterAsync(string? role)
    {
        var query = DbSet.AsQueryable();
        if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<Enums.UserRole>(role, true, out var parsedRole))
        {
            query = query.Where(u => u.Role == parsedRole);
        }
        return await query.OrderByDescending(u => u.CreatedAtUtc).ToListAsync();
    }
}
