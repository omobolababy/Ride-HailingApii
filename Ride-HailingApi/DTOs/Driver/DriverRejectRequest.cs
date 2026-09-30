using System.ComponentModel.DataAnnotations;

namespace Ride_HailingApi.DTOs.Driver;

public class DriverRejectRequest
{
    [Required, MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}