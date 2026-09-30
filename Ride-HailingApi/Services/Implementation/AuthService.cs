using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ride_HailingApi.DTOs.Auth;
using Ride_HailingApi.Entities;
using Ride_HailingApi.Enums;
using Ride_HailingApi.Helpers;
using Ride_HailingApi.Repositories.Interface;
using Ride_HailingApi.Services.Interface;
using Ride_HailingApi.Utils;
using RideHailingApi.DTOs.Common;


namespace RideHailingApi.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IOtpRepository _otpRepository;
    private readonly IOtpService _otpService;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly IAuditService _auditService;
    private readonly ILogger<AuthService> _logger;
    private readonly OtpSettings _otpSettings;

    public AuthService(IUserRepository userRepository, IOtpRepository otpRepository, IOtpService otpService,
        ITokenService tokenService, IEmailService emailService, ISmsService smsService,
        IAuditService auditService, IOptions<OtpSettings> otpSettings, ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _otpRepository = otpRepository;
        _otpService = otpService;
        _tokenService = tokenService;
        _emailService = emailService;
        _smsService = smsService;
        _auditService = auditService;
        _logger = logger;
        _otpSettings = otpSettings.Value;
    }

    public async Task<ApiResponse<string>> RegisterAsync(RegisterRequest request)
    {
        if (request.Role == UserRole.Admin)
        {
            _logger.LogWarning("Registration rejected: attempt to self-register an Admin account.");
            return ApiResponse<string>.FailResponse("Admin accounts cannot be self-registered.");
        }

        if (await _userRepository.EmailExistsAsync(request.Email))
        {
            _logger.LogWarning("Registration rejected: email address is already registered.");
            return ApiResponse<string>.FailResponse("An account with this email already exists.");
        }

        if (await _userRepository.PhoneNumberExistsAsync(request.PhoneNumber))
        {
            _logger.LogWarning("Registration rejected: phone number is already registered.");
            return ApiResponse<string>.FailResponse("An account with this phone number already exists.");
        }

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email.Trim().ToLower(),
            PhoneNumber = request.PhoneNumber,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role
        };

        try
        {
            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Two simultaneous registrations can both pass the exists-checks; the unique index then rejects the second.
            _logger.LogWarning(ex, "Registration failed on save, most likely a duplicate email or phone number.");
            return ApiResponse<string>.FailResponse("An account with this email or phone number already exists.");
        }

        _logger.LogInformation("User {UserId} registered with role {Role}.", user.Id, user.Role);

        await SendEmailOtpAsync(user, OtpPurpose.EmailVerification);

        await _auditService.LogAsync(user.Id, user.Role.ToString(), "Registration",
            "User", user.Id.ToString(), $"New {user.Role} account registered with email {user.Email}");

        return ApiResponse<string>.SuccessResponse(
            "Registration successful. Please check your email for a verification code.");
    }

    public async Task<ApiResponse<string>> VerifyEmailAsync(VerifyEmailRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user is null)
        {
            _logger.LogWarning("Email verification failed: no matching account.");
            return ApiResponse<string>.FailResponse("Invalid email or OTP.");
        }

        if (user.IsEmailVerified)
        {
            return ApiResponse<string>.SuccessResponse("Email is already verified.");
        }

        var otp = await _otpRepository.GetValidEmailOtpAsync(user.Id, request.Otp, OtpPurpose.EmailVerification);
        if (otp is null)
        {
            _logger.LogWarning("Email verification failed for user {UserId}: invalid or expired OTP.", user.Id);
            await _auditService.LogAsync(user.Id, user.Role.ToString(), "EmailVerification", "User",
                user.Id.ToString(), "Invalid or expired OTP", wasSuccessful: false);
            return ApiResponse<string>.FailResponse("Invalid or expired OTP.");
        }

        otp.IsUsed = true;
        user.IsEmailVerified = true;
        user.UpdatedAtUtc = DateTime.UtcNow;
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync();

        await _auditService.LogAsync(user.Id, user.Role.ToString(), "EmailVerification", "User", user.Id.ToString());
        _logger.LogInformation("User {UserId} verified their email address.", user.Id);

        return ApiResponse<string>.SuccessResponse("Email verified successfully.");
    }

    public async Task<ApiResponse<string>> VerifyPhoneAsync(VerifyPhoneRequest request)
    {
        var user = await _userRepository.GetByPhoneNumberAsync(request.PhoneNumber);
        if (user is null)
        {
            _logger.LogWarning("Phone verification failed: no matching account.");
            return ApiResponse<string>.FailResponse("Invalid phone number or OTP.");
        }

        if (user.IsPhoneVerified)
        {
            return ApiResponse<string>.SuccessResponse("Phone number is already verified.");
        }

        var otp = await _otpRepository.GetValidPhoneOtpAsync(user.Id, request.Otp);
        if (otp is null)
        {
            _logger.LogWarning("Phone verification failed for user {UserId}: invalid or expired OTP.", user.Id);
            await _auditService.LogAsync(user.Id, user.Role.ToString(), "PhoneVerification", "User",
                user.Id.ToString(), "Invalid or expired OTP", wasSuccessful: false);
            return ApiResponse<string>.FailResponse("Invalid or expired OTP.");
        }

        otp.IsUsed = true;
        user.IsPhoneVerified = true;
        user.UpdatedAtUtc = DateTime.UtcNow;
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync();

        await _auditService.LogAsync(user.Id, user.Role.ToString(), "PhoneVerification", "User", user.Id.ToString());
        _logger.LogInformation("User {UserId} verified their phone number.", user.Id);

        return ApiResponse<string>.SuccessResponse("Phone number verified successfully.");
    }

    public async Task<ApiResponse<string>> ResendEmailOtpAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        // Do not reveal whether the account exists.
        if (user is not null && !user.IsEmailVerified)
        {
            await SendEmailOtpAsync(user, OtpPurpose.EmailVerification);
            _logger.LogInformation("Email verification OTP resent to user {UserId}.", user.Id);
        }
        return ApiResponse<string>.SuccessResponse("If the account exists and is unverified, a new OTP has been sent.");
    }

    public async Task<ApiResponse<string>> ResendPhoneOtpAsync(string phoneNumber)
    {
        var user = await _userRepository.GetByPhoneNumberAsync(phoneNumber);
        if (user is not null && !user.IsPhoneVerified)
        {
            await SendPhoneOtpAsync(user);
            _logger.LogInformation("Phone verification OTP resent to user {UserId}.", user.Id);
        }
        return ApiResponse<string>.SuccessResponse("If the account exists and is unverified, a new OTP has been sent.");
    }

    public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt (matched user id: {UserId}).", user?.Id);
            await _auditService.LogAsync(user?.Id, user?.Role.ToString() ?? "Unknown", "LoginAttempt",
                "User", user?.Id.ToString(), $"Failed login for {request.Email}", wasSuccessful: false);
            return ApiResponse<LoginResponse>.FailResponse("Invalid email or password.");
        }

        if (!user.IsEmailVerified)
        {
            _logger.LogWarning("Login blocked for user {UserId}: email address not verified.", user.Id);
            return ApiResponse<LoginResponse>.FailResponse("Please verify your email address before logging in.");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login blocked for user {UserId}: account is deactivated.", user.Id);
            await _auditService.LogAsync(user.Id, user.Role.ToString(), "LoginAttempt", "User",
                user.Id.ToString(), "Login attempt on deactivated account", wasSuccessful: false);
            return ApiResponse<LoginResponse>.FailResponse("Your account has been deactivated. Please contact support.");
        }

        var (token, expiresAtUtc) = _tokenService.GenerateToken(user);

        await _auditService.LogAsync(user.Id, user.Role.ToString(), "LoginAttempt", "User", user.Id.ToString(),
            "Successful login");
        _logger.LogInformation("User {UserId} logged in as {Role}.", user.Id, user.Role);

        var response = new LoginResponse
        {
            Token = token,
            ExpiresAtUtc = expiresAtUtc,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString()
        };

        return ApiResponse<LoginResponse>.SuccessResponse(response, "Login successful.");
    }

    public async Task<ApiResponse<string>> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);

        // Per spec: never reveal whether the email exists.
        if (user is not null)
        {
            var code = _otpService.GenerateOtpCode();
            await _otpRepository.InvalidatePreviousPasswordResetOtpsAsync(user.Id);
            await _otpRepository.AddPasswordResetOtpAsync(new PasswordResetOtp
            {
                UserId = user.Id,
                Code = code,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(_otpSettings.ExpiryMinutes)
            });
            await _otpRepository.SaveChangesAsync();

            await _emailService.SendPasswordResetOtpAsync(user.Email, user.FullName, code, _otpSettings.ExpiryMinutes);

            await _auditService.LogAsync(user.Id, user.Role.ToString(), "PasswordResetRequested", "User", user.Id.ToString());
            _logger.LogInformation("Password reset OTP issued for user {UserId}.", user.Id);
        }

        return ApiResponse<string>.SuccessResponse(
            "If an account with this email exists, a password reset code has been sent.");
    }

    public async Task<ApiResponse<string>> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user is null)
        {
            _logger.LogWarning("Password reset failed: no matching account.");
            return ApiResponse<string>.FailResponse("Invalid request.");
        }

        var otp = await _otpRepository.GetValidPasswordResetOtpAsync(user.Id, request.Otp);
        if (otp is null)
        {
            _logger.LogWarning("Password reset failed for user {UserId}: invalid or expired OTP.", user.Id);
            await _auditService.LogAsync(user.Id, user.Role.ToString(), "PasswordReset", "User", user.Id.ToString(),
                "Invalid or expired OTP", wasSuccessful: false);
            return ApiResponse<string>.FailResponse("Invalid or expired OTP.");
        }

        otp.IsUsed = true;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAtUtc = DateTime.UtcNow;
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync();

        await _emailService.SendPasswordChangedNoticeAsync(user.Email, user.FullName);

        await _auditService.LogAsync(user.Id, user.Role.ToString(), "PasswordReset", "User", user.Id.ToString());
        _logger.LogInformation("Password reset completed for user {UserId}.", user.Id);

        return ApiResponse<string>.SuccessResponse("Password has been reset successfully.");
    }

    public async Task<ApiResponse<string>> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
        {
            _logger.LogWarning("Password change failed: user {UserId} not found.", userId);
            return ApiResponse<string>.FailResponse("User not found.");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            _logger.LogWarning("Password change failed for user {UserId}: incorrect current password.", user.Id);
            await _auditService.LogAsync(user.Id, user.Role.ToString(), "PasswordChange", "User", user.Id.ToString(),
                "Incorrect current password", wasSuccessful: false);
            return ApiResponse<string>.FailResponse("Current password is incorrect.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAtUtc = DateTime.UtcNow;
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync();

        await _emailService.SendPasswordChangedNoticeAsync(user.Email, user.FullName);

        await _auditService.LogAsync(user.Id, user.Role.ToString(), "PasswordChange", "User", user.Id.ToString());
        _logger.LogInformation("User {UserId} changed their password.", user.Id);

        return ApiResponse<string>.SuccessResponse("Password changed successfully.");
    }

    // ---------- Private helpers ----------

    private async Task SendEmailOtpAsync(User user, OtpPurpose purpose)
    {
        var code = _otpService.GenerateOtpCode();
        await _otpRepository.InvalidatePreviousEmailOtpsAsync(user.Id, purpose);
        await _otpRepository.AddEmailOtpAsync(new EmailOtp
        {
            UserId = user.Id,
            Code = code,
            Purpose = purpose,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(_otpSettings.ExpiryMinutes)
        });
        await _otpRepository.SaveChangesAsync();

        await _emailService.SendEmailVerificationOtpAsync(user.Email, user.FullName, code, _otpSettings.ExpiryMinutes);

        // Kick off phone verification too, since both are required by the spec.
        if (!user.IsPhoneVerified)
        {
            await SendPhoneOtpAsync(user);
        }
    }

    private async Task SendPhoneOtpAsync(User user)
    {
        var code = _otpService.GenerateOtpCode();
        await _otpRepository.InvalidatePreviousPhoneOtpsAsync(user.Id);
        await _otpRepository.AddPhoneOtpAsync(new PhoneOtp
        {
            UserId = user.Id,
            Code = code,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(_otpSettings.ExpiryMinutes)
        });
        await _otpRepository.SaveChangesAsync();

        var message = SmsUtils.PhoneVerificationOtp(code, _otpSettings.ExpiryMinutes);
        await _smsService.SendSmsAsync(user.Id, user.PhoneNumber, message);
    }
}
