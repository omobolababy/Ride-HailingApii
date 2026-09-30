namespace Ride_HailingApi.DTOs.Driver;

public class VehicleResponse
{
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Color { get; set; } = string.Empty;
    public string PlateNumber { get; set; } = string.Empty;  
}