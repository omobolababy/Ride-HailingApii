namespace Ride_HailingApi.Entities;

public class PasswordResetOtp
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string Code { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }
    public bool IsUsed { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}