using Ride_HailingApi.Entities;

namespace Ride_HailingApi.Repositories.Interface;

public interface IAuditLogRepository : IGenericRepository<AuditLog>
{
    Task<(IEnumerable<AuditLog> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize);   
}