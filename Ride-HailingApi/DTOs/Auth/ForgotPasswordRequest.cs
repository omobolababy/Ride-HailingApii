using System.ComponentModel.DataAnnotations;

namespace Ride_HailingApi.DTOs.Auth;

public class ForgotPasswordRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}