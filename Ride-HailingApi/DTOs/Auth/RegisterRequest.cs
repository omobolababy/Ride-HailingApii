using System.ComponentModel.DataAnnotations;
using Ride_HailingApi.Enums;

namespace Ride_HailingApi.DTOs.Auth;

public class RegisterRequest
{
    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    // Only Passenger or Driver may self-register. Admin accounts are provisioned separately.
    [Required]
    public UserRole Role { get; set; }
}