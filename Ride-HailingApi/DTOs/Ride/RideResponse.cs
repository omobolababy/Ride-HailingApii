namespace Ride_HailingApi.DTOs.Ride;

public class RideResponse
{
    public int Id { get; set; }
    public string RideReference { get; set; } = string.Empty;
    public int PassengerId { get; set; }
    public string PassengerName { get; set; } = string.Empty;
    public int? DriverProfileId { get; set; }
    public string? DriverName { get; set; }
    public string PickupLocation { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal? EstimatedFare { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public DateTime? AcceptedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public string? CancellationReason { get; set; } 
}