using System.ComponentModel.DataAnnotations;

namespace Ride_HailingApi.DTOs.Admin;

public class SetActiveStatusRequest
{
    [Required]
    public bool IsActive { get; set; }
}