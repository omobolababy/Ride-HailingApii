using Ride_HailingApi.DTOs.Auth;
using RideHailingApi.DTOs.Common;

namespace Ride_HailingApi.Services.Interface;

public interface IAuthService
{
    Task<ApiResponse<string>> RegisterAsync(RegisterRequest request);
    Task<ApiResponse<string>> VerifyEmailAsync(VerifyEmailRequest request);
    Task<ApiResponse<string>> VerifyPhoneAsync(VerifyPhoneRequest request);
    Task<ApiResponse<string>> ResendEmailOtpAsync(string email);
    Task<ApiResponse<string>> ResendPhoneOtpAsync(string phoneNumber);
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request);
    Task<ApiResponse<string>> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<ApiResponse<string>> ResetPasswordAsync(ResetPasswordRequest request);
    Task<ApiResponse<string>> ChangePasswordAsync(int userId, ChangePasswordRequest request);

}