using Ride_HailingApi.Enums;

namespace Ride_HailingApi.Entities;

public class EmailOtp
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string Code { get; set; } = string.Empty;
    public OtpPurpose Purpose { get; set; } = OtpPurpose.EmailVerification;

    public DateTime ExpiresAtUtc { get; set; }
    public bool IsUsed { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}