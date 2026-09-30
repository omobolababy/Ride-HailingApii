using Ride_HailingApi.Enums;

namespace Ride_HailingApi.Entities;

public class Ride
{
    public int Id { get; set; }
    public string RideReference { get; set; } = string.Empty;

    public int PassengerId { get; set; }
    public User Passenger { get; set; } = null!;

    public int? DriverProfileId { get; set; }
    public DriverProfile? DriverProfile { get; set; }
    public string PickupLocation { get; set; }
    public string Destination { get; set; }
    public RideStatus Status { get; set; } = RideStatus.Requested;
    public decimal? EstimatedFare { get; set; }

    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public int? CancelledByUserId { get; set; }
    public string? CancellationReason { get; set; }
    
    /// <summary>Concurrency token so two drivers cannot both accept the same ride.</summary>
    [System.ComponentModel.DataAnnotations.Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<RideStatusHistory> StatusHistories { get; set; } = new List<RideStatusHistory>();
}