namespace Ride_HailingApi.DTOs.Driver;

public class DriverResponse
{
    public int DriverProfileId { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public VehicleResponse? Vehicle { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}