using System.ComponentModel.DataAnnotations;

namespace Ride_HailingApi.DTOs.Auth;

public class VerifyPhoneRequest
{
    [Required, Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, StringLength(6, MinimumLength = 6)]
    public string Otp { get; set; } = string.Empty;
}