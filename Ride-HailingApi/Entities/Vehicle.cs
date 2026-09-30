namespace Ride_HailingApi.Entities;

public class Vehicle
{
    public int Id { get; set; }

    public int DriverProfileId { get; set; }
    public DriverProfile DriverProfile { get; set; } = null!;

    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Color { get; set; } = string.Empty;
    public string PlateNumber { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}