using Microsoft.EntityFrameworkCore;
using Ride_HailingApi.Entities;
using Ride_HailingApi.Enums;
using Ride_HailingApi.Repositories.Interface;
using RideHailingApi.Data;

namespace Ride_HailingApi.Repositories.Implementation;

public class OtpRepository : IOtpRepository
{
    private readonly ApplicationDbContext _context;

    public OtpRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // ---------- Email OTPs ----------
    public async Task AddEmailOtpAsync(EmailOtp otp) => await _context.EmailOtps.AddAsync(otp);

    public async Task<EmailOtp?> GetValidEmailOtpAsync(int userId, string code, OtpPurpose purpose) =>
        await _context.EmailOtps
            .Where(o => o.UserId == userId && o.Code == code && o.Purpose == purpose &&
                        !o.IsUsed && o.ExpiresAtUtc > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAtUtc)
            .FirstOrDefaultAsync();

    public async Task InvalidatePreviousEmailOtpsAsync(int userId, OtpPurpose purpose)
    {
        var otps = await _context.EmailOtps
            .Where(o => o.UserId == userId && o.Purpose == purpose && !o.IsUsed)
            .ToListAsync();
        foreach (var otp in otps) otp.IsUsed = true;
    }

    // ---------- Phone OTPs ----------
    public async Task AddPhoneOtpAsync(PhoneOtp otp) => await _context.PhoneOtps.AddAsync(otp);

    public async Task<PhoneOtp?> GetValidPhoneOtpAsync(int userId, string code) =>
        await _context.PhoneOtps
            .Where(o => o.UserId == userId && o.Code == code && !o.IsUsed && o.ExpiresAtUtc > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAtUtc)
            .FirstOrDefaultAsync();

    public async Task InvalidatePreviousPhoneOtpsAsync(int userId)
    {
        var otps = await _context.PhoneOtps.Where(o => o.UserId == userId && !o.IsUsed).ToListAsync();
        foreach (var otp in otps) otp.IsUsed = true;
    }

    // ---------- Password reset OTPs ----------
    public async Task AddPasswordResetOtpAsync(PasswordResetOtp otp) => await _context.PasswordResetOtps.AddAsync(otp);

    public async Task<PasswordResetOtp?> GetValidPasswordResetOtpAsync(int userId, string code) =>
        await _context.PasswordResetOtps
            .Where(o => o.UserId == userId && o.Code == code && !o.IsUsed && o.ExpiresAtUtc > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAtUtc)
            .FirstOrDefaultAsync();

    public async Task InvalidatePreviousPasswordResetOtpsAsync(int userId)
    {
        var otps = await _context.PasswordResetOtps.Where(o => o.UserId == userId && !o.IsUsed).ToListAsync();
        foreach (var otp in otps) otp.IsUsed = true;
    }

    public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();
}
