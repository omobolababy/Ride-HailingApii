using Microsoft.EntityFrameworkCore;
using Ride_HailingApi.Entities;
using Ride_HailingApi.Repositories.Interface;
using RideHailingApi.Data;

namespace Ride_HailingApi.Repositories.Implementation;

public class AuditLogRepository : GenericRepository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(ApplicationDbContext context) : base(context) { }

    public async Task<(IEnumerable<AuditLog> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize)
    {
        var query = DbSet.OrderByDescending(a => a.CreatedAtUtc);
        var totalCount = await query.CountAsync();
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, totalCount);
    }
}

