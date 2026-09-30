using Ride_HailingApi.Entities;
using Ride_HailingApi.Enums;

namespace Ride_HailingApi.Repositories.Interface;

public interface IOtpRepository
{
    // Email OTPs (registration verification + can double as generic-purpose OTPs)
    Task AddEmailOtpAsync(EmailOtp otp);
    Task<EmailOtp?> GetValidEmailOtpAsync(int userId, string code, OtpPurpose purpose);
    Task InvalidatePreviousEmailOtpsAsync(int userId, OtpPurpose purpose);

    // Phone OTPs
    Task AddPhoneOtpAsync(PhoneOtp otp);
    Task<PhoneOtp?> GetValidPhoneOtpAsync(int userId, string code);
    Task InvalidatePreviousPhoneOtpsAsync(int userId);

    // Password reset OTPs
    Task AddPasswordResetOtpAsync(PasswordResetOtp otp);
    Task<PasswordResetOtp?> GetValidPasswordResetOtpAsync(int userId, string code);
    Task InvalidatePreviousPasswordResetOtpsAsync(int userId);

    Task<int> SaveChangesAsync();
}