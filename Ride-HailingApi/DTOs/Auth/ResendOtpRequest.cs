using System.ComponentModel.DataAnnotations;

namespace Ride_HailingApi.DTOs.Auth;

public class ResendEmailOtpRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class ResendPhoneOtpRequest
{
    [Required, Phone]
    public string PhoneNumber { get; set; } = string.Empty;
}