using Ride_HailingApi.Enums;

namespace Ride_HailingApi.Entities;

public class DriverProfile
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string LicenseNumber { get; set; } = string.Empty;
    public DateTime LicenseExpiryDate { get; set; }
    public DriverApprovalStatus ApprovalStatus { get; set; } = DriverApprovalStatus.Pending;
    public string? RejectionReason { get; set; }
    public int? ApprovedByAdminId { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public bool IsAvailable { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public Vehicle? Vehicle { get; set; }
    public ICollection<Ride> RidesAsDriver { get; set; }
}