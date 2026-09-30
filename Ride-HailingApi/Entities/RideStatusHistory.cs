using Ride_HailingApi.Enums;

namespace Ride_HailingApi.Entities;

public class RideStatusHistory
{
    public int Id { get; set; }

    public int RideId { get; set; }
    public Ride Ride { get; set; } = null!;

    public RideStatus FromStatus { get; set; }
    public RideStatus ToStatus { get; set; }

    public int? ChangedByUserId { get; set; }
    public string? Notes { get; set; }

    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
}