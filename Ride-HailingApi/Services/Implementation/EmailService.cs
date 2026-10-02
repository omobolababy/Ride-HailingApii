using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using Ride_HailingApi.Helpers;
using Ride_HailingApi.Services.Interface;
using Ride_HailingApi.Utils;

namespace Ride_HailingApi.Services.Implementation;

public class EmailService : IEmailService
{
    private readonly SmtpMail _smtpMail;
    private readonly ILogger<EmailService> _logger;
    private readonly IHostEnvironment _environment;

    public EmailService(
        IOptions<SmtpMail> smtpMail,
        ILogger<EmailService> logger,
        IHostEnvironment environment)
    {
        _smtpMail = smtpMail.Value;
        _logger = logger;
        _environment = environment;
    }

    // ---------- Transport ----------

    private async Task SendMimeMessageAsync(MimeMessage message)
    {
        _logger.LogInformation("Sending email to {Recipient} - Subject: {Subject}", message.To, message.Subject);

        using var client = new SmtpClient();

        try
        {
            // STARTTLS on port 587
            await client.ConnectAsync(_smtpMail.Server, _smtpMail.Port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_smtpMail.Username, _smtpMail.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully to {Recipient}", message.To);
        }
        catch (Exception ex) when (ex is AuthenticationException
                                       or SmtpCommandException
                                       or SmtpProtocolException)
        {
            // The account was already created, so a failed send must not fail the whole request;
            // the user can use the resend-OTP endpoints. Check the SMTP credentials in appsettings.
            _logger.LogError(ex,
                "SMTP error while sending email to {Recipient}. The email was NOT sent. Check the SMTP credentials.",
                message.To);

            // Development convenience only: surface the email body (which contains the OTP) in the console.
            // Never do this outside Development, since logs would then hold live verification codes.
            if (_environment.IsDevelopment())
            {
                _logger.LogWarning("EMAIL CONTENT FALLBACK (DEV ONLY)\nSubject: {Subject}\nBody: {Body}",
                    message.Subject, GetBodyText(message));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while sending email to {Recipient}", message.To);
            throw;
        }
    }

    private static string GetBodyText(MimeMessage message) => message.Body switch
    {
        TextPart text => text.Text,
        Multipart multipart =>
            multipart.OfType<TextPart>().FirstOrDefault(x => x.IsHtml)?.Text
            ?? multipart.OfType<TextPart>().FirstOrDefault(x => x.IsPlain)?.Text
            ?? "Complex multipart body",
        _ => string.Empty
    };

    private MimeMessage CreateBaseMessage(string toEmail, string toName, string subject, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtpMail.SenderName, _smtpMail.SenderEmail));
        message.To.Add(new MailboxAddress(string.IsNullOrWhiteSpace(toName) ? toEmail : toName, toEmail));
        message.Subject = subject;
        message.Body = new TextPart(TextFormat.Html) { Text = htmlBody };
        return message;
    }

    private Task SendAsync(string toEmail, string toName, (string Subject, string Body) email) =>
        SendMimeMessageAsync(CreateBaseMessage(toEmail, toName, email.Subject, email.Body));

    // ---------- IEmailService ----------

    public Task SendEmailVerificationOtpAsync(string toEmail, string fullName, string otp, int expiryMinutes) =>
        SendAsync(toEmail, fullName, EmailUtils.EmailVerificationOtp(fullName, otp, expiryMinutes));

    public Task SendPasswordResetOtpAsync(string toEmail, string fullName, string otp, int expiryMinutes) =>
        SendAsync(toEmail, fullName, EmailUtils.PasswordResetOtp(fullName, otp, expiryMinutes));

    public Task SendPasswordChangedNoticeAsync(string toEmail, string fullName) =>
        SendAsync(toEmail, fullName, EmailUtils.PasswordChangedNotice(fullName));

    public Task SendDriverApplicationApprovedAsync(string toEmail, string fullName) =>
        SendAsync(toEmail, fullName, EmailUtils.DriverApplicationApproved(fullName));

    public Task SendDriverApplicationRejectedAsync(string toEmail, string fullName, string reason) =>
        SendAsync(toEmail, fullName, EmailUtils.DriverApplicationRejected(fullName, reason));

    public Task SendRideAcceptedByDriverAsync(string toEmail, string passengerName, string driverName,
        string vehicleInfo, string plateNumber, string rideReference) =>
        SendAsync(toEmail, passengerName,
            EmailUtils.RideAcceptedByDriver(passengerName, driverName, vehicleInfo, plateNumber, rideReference));

    public Task SendDriverArrivedAsync(string toEmail, string passengerName, string rideReference) =>
        SendAsync(toEmail, passengerName, EmailUtils.DriverArrived(passengerName, rideReference));

    public Task SendRideCancelledAsync(string toEmail, string recipientName, string rideReference, string reason) =>
        SendAsync(toEmail, recipientName, EmailUtils.RideCancelled(recipientName, rideReference, reason));

    public Task SendRideCompletedAsync(string toEmail, string passengerName, string rideReference, decimal? fare) =>
        SendAsync(toEmail, passengerName, EmailUtils.RideCompleted(passengerName, rideReference, fare));
}
