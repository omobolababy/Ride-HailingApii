using System.ComponentModel.DataAnnotations;

namespace Ride_HailingApi.DTOs.Driver;

public class DriverAvailabilityRequest
{
    [Required]
    public bool IsAvailable { get; set; }
}