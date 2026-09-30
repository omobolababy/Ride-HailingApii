using System.ComponentModel.DataAnnotations;
using Ride_HailingApi.Enums;

namespace Ride_HailingApi.DTOs.Ride;

public class RideStatusUpdateRequest
{
    [Required]
    public RideStatus NewStatus { get; set; }
    public string? Notes { get; set; } 
}