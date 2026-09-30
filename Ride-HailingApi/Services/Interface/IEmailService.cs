namespace Ride_HailingApi.Services.Interface;

public interface IEmailService
{
    Task SendEmailVerificationOtpAsync(string toEmail, string fullName, string otp, int expiryMinutes);
    Task SendPasswordResetOtpAsync(string toEmail, string fullName, string otp, int expiryMinutes);
    Task SendPasswordChangedNoticeAsync(string toEmail, string fullName);

    Task SendDriverApplicationApprovedAsync(string toEmail, string fullName);
    Task SendDriverApplicationRejectedAsync(string toEmail, string fullName, string reason);

    Task SendRideAcceptedByDriverAsync(
        string toEmail,
        string passengerName,
        string driverName,
        string vehicleInfo,
        string plateNumber,
        string rideReference);

    Task SendDriverArrivedAsync(string toEmail, string passengerName, string rideReference);

    Task SendRideCancelledAsync(
        string toEmail,
        string recipientName,
        string rideReference,
        string reason);

    Task SendRideCompletedAsync(
        string toEmail,
        string passengerName,
        string rideReference,
        decimal? fare);
}