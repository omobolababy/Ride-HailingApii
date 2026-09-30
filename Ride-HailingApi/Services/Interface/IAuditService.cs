namespace Ride_HailingApi.Services.Interface;

public interface IAuditService
{
    Task LogAsync(int? actorUserId, string actorRole, string action, string? entityType = null,
        string? entityId = null, string? details = null, bool wasSuccessful = true);

}