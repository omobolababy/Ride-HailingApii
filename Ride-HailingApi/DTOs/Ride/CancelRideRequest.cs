using System.ComponentModel.DataAnnotations;

namespace Ride_HailingApi.DTOs.Ride;

public class CancelRideRequest
{
    [Required, MaxLength(300)]
    public string Reason { get; set; } = string.Empty;
}