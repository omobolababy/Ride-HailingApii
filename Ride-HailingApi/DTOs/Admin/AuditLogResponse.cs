namespace Ride_HailingApi.DTOs.Admin;

public class AuditLogResponse
{
    public int Id { get; set; }
    public int? ActorUserId { get; set; }
    public string ActorRole { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public bool WasSuccessful { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}