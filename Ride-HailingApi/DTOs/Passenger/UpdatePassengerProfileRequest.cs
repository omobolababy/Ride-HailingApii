using System.ComponentModel.DataAnnotations;

namespace Ride_HailingApi.DTOs.Passenger;

public class UpdatePassengerProfileRequest
{
    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, Phone]
    public string PhoneNumber { get; set; } = string.Empty;
}