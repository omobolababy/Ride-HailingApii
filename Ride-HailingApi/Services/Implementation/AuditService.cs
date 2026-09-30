using Ride_HailingApi.Entities;
using Ride_HailingApi.Repositories.Interface;
using Ride_HailingApi.Services.Interface;

namespace Ride_HailingApi.Services.Implementation;

public class AuditService : IAuditService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IAuditLogRepository auditLogRepository, ILogger<AuditService> logger)
    {
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    public async Task LogAsync(int? actorUserId, string actorRole, string action, string? entityType = null,
        string? entityId = null, string? details = null, bool wasSuccessful = true)
    {
        try
        {
            var log = new AuditLog
            {
                ActorUserId = actorUserId,
                ActorRole = actorRole,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                WasSuccessful = wasSuccessful,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _auditLogRepository.AddAsync(log);
            await _auditLogRepository.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Audit logging must never break the main request flow.
            _logger.LogError(ex, "Failed to write audit log for action {Action}", action);
        }
    }
}