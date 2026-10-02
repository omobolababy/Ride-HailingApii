using System.Security.Claims;
using Ride_HailingApi.DTOs.Auth;
using Ride_HailingApi.Helpers;
using Ride_HailingApi.Services.Interface;
using RideHailingApi.DTOs.Common;

namespace Ride_HailingApi.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", async (RegisterRequest request, IAuthService auth) =>
            (await auth.RegisterAsync(request)).ToResult())
            .WithValidation<RegisterRequest>()
            .WithName("Register")
            .Produces<ApiResponse<string>>(ResponseCodes.Created);

        group.MapPost("/verify-email", async (VerifyEmailRequest request, IAuthService auth) =>
            (await auth.VerifyEmailAsync(request)).ToResult())
            .WithValidation<VerifyEmailRequest>()
            .WithName("VerifyEmail");

        group.MapPost("/verify-phone", async (VerifyPhoneRequest request, IAuthService auth) =>
            (await auth.VerifyPhoneAsync(request)).ToResult())
            .WithValidation<VerifyPhoneRequest>()
            .WithName("VerifyPhone");

        group.MapPost("/resend-email-otp", async (string email, IAuthService auth) =>
            (await auth.ResendEmailOtpAsync(email)).ToResult())
            .WithName("ResendEmailOtp");

        group.MapPost("/resend-phone-otp", async (string phoneNumber, IAuthService auth) =>
            (await auth.ResendPhoneOtpAsync(phoneNumber)).ToResult())
            .WithName("ResendPhoneOtp");

        group.MapPost("/login", async (LoginRequest request, IAuthService auth) =>
            (await auth.LoginAsync(request)).ToResult())
            .WithValidation<LoginRequest>()
            .WithName("Login");

        // Always returns 200 with a generic message so an email's existence cannot be inferred.
        group.MapPost("/forgot-password", async (ForgotPasswordRequest request, IAuthService auth) =>
            (await auth.ForgotPasswordAsync(request)).ToResult())
            .WithValidation<ForgotPasswordRequest>()
            .WithName("ForgotPassword");

        group.MapPost("/reset-password", async (ResetPasswordRequest request, IAuthService auth) =>
            (await auth.ResetPasswordAsync(request)).ToResult())
            .WithValidation<ResetPasswordRequest>()
            .WithName("ResetPassword");

        group.MapPost("/change-password", async (ChangePasswordRequest request, ClaimsPrincipal user, IAuthService auth) =>
            (await auth.ChangePasswordAsync(user.GetUserId(), request)).ToResult())
            .WithValidation<ChangePasswordRequest>()
            .RequireAuthorization()
            .WithName("ChangePassword");

        return app;
    }
}
