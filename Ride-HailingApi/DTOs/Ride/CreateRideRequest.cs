using System.ComponentModel.DataAnnotations;

namespace Ride_HailingApi.DTOs.Ride;

public class CreateRideRequest
{
    [Required, MaxLength(300)]
    public string PickupLocation { get; set; } = string.Empty;
    
    [Required, MaxLength(300)]
    public string Destination { get; set; } = string.Empty;
}