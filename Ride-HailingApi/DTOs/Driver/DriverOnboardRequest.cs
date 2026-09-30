using System.ComponentModel.DataAnnotations;

namespace Ride_HailingApi.DTOs.Driver;

public class DriverOnboardRequest
{
    [Required, MaxLength(50)]
    public string LicenseNumber { get; set; } = string.Empty;

    [Required]
    public DateTime LicenseExpiryDate { get; set; }

    [Required, MaxLength(50)]
    public string VehicleMake { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string VehicleModel { get; set; } = string.Empty;

    [Required, Range(1980, 2100)]
    public int VehicleYear { get; set; }

    [Required, MaxLength(30)]
    public string VehicleColor { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string PlateNumber { get; set; } = string.Empty; 
}