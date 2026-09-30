namespace Ride_HailingApi.Utils;

public static class EmailUtils
{
    private const string PrimaryColor = "#0B5FFF";   // brand blue
    private const string AccentColor = "#FFB020";    // amber accent (taxi/ride theme)
    private const string DarkText = "#1A1A2E";
    private const string MutedText = "#6B7280";

    private static string Layout(string title, string bodyHtml, string footerNote = "")
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8' />
<meta name='viewport' content='width=device-width, initial-scale=1.0' />
<title>{title}</title>
</head>
<body style='margin:0; padding:0; background-color:#F3F4F6; font-family: Segoe UI, Arial, sans-serif;'>
  <table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='background-color:#F3F4F6; padding: 24px 0;'>
    <tr>
      <td align='center'>
        <table role='presentation' width='480' cellpadding='0' cellspacing='0' style='background-color:#FFFFFF; border-radius:10px; overflow:hidden; box-shadow: 0 1px 4px rgba(0,0,0,0.08);'>
          <tr>
            <td style='background-color:{PrimaryColor}; padding: 20px 28px;'>
              <span style='color:#FFFFFF; font-size:20px; font-weight:700; letter-spacing:0.5px;'>🚗 Ride-Hailing</span>
            </td>
          </tr>
          <tr>
            <td style='padding: 28px;'>
              {bodyHtml}
            </td>
          </tr>
          <tr>
            <td style='padding: 16px 28px; background-color:#FAFAFA; border-top:1px solid #EEEEEE;'>
              <p style='margin:0; font-size:12px; color:{MutedText};'>{footerNote}</p>
              <p style='margin:6px 0 0; font-size:12px; color:{MutedText};'>This is an automated message from the Ride-Hailing API. Please do not reply to this email.</p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
    }

    private static string OtpBox(string otp)
    {
        return $@"
<div style='text-align:center; margin: 24px 0;'>
  <span style='display:inline-block; padding: 14px 28px; background-color:#F3F4F6; border: 1px dashed {PrimaryColor}; border-radius:8px; font-size:28px; font-weight:700; letter-spacing:8px; color:{DarkText};'>{otp}</span>
</div>";
    }

    public static (string Subject, string Body) EmailVerificationOtp(string fullName, string otp, int expiryMinutes)
    {
        var subject = "Verify your email address";
        var body = Layout(subject, $@"
            <h2 style='margin:0 0 12px; color:{DarkText};'>Hi {fullName},</h2>
            <p style='margin:0 0 8px; color:{DarkText}; font-size:15px;'>Welcome aboard! Use the code below to verify your email address.</p>
            {OtpBox(otp)}
            <p style='margin:0; color:{MutedText}; font-size:13px; text-align:center;'>This code expires in {expiryMinutes} minutes and can only be used once.</p>
        ", "If you didn't create this account, you can safely ignore this email.");
        return (subject, body);
    }

    public static (string Subject, string Body) PasswordResetOtp(string fullName, string otp, int expiryMinutes)
    {
        var subject = "Reset your password";
        var body = Layout(subject, $@"
            <h2 style='margin:0 0 12px; color:{DarkText};'>Hi {fullName},</h2>
            <p style='margin:0 0 8px; color:{DarkText}; font-size:15px;'>We received a request to reset your password. Use the code below to continue.</p>
            {OtpBox(otp)}
            <p style='margin:0; color:{MutedText}; font-size:13px; text-align:center;'>This code expires in {expiryMinutes} minutes and can only be used once.</p>
        ", "If you didn't request a password reset, please ignore this email or contact support — your account is still secure.");
        return (subject, body);
    }

    public static (string Subject, string Body) PasswordChangedNotice(string fullName)
    {
        var subject = "Your password was changed";
        var body = Layout(subject, $@"
            <h2 style='margin:0 0 12px; color:{DarkText};'>Hi {fullName},</h2>
            <p style='margin:0 0 8px; color:{DarkText}; font-size:15px;'>This is a confirmation that your account password was successfully changed on {DateTime.UtcNow:dd MMM yyyy, HH:mm} UTC.</p>
            <p style='margin:0; color:{MutedText}; font-size:13px;'>If you did not make this change, please contact support immediately.</p>
        ");
        return (subject, body);
    }

    public static (string Subject, string Body) DriverApplicationApproved(string fullName)
    {
        var subject = "Your driver application has been approved";
        var body = Layout(subject, $@"
            <h2 style='margin:0 0 12px; color:{DarkText};'>Congratulations {fullName}!</h2>
            <p style='margin:0; color:{DarkText}; font-size:15px;'>Your driver application has been reviewed and <strong style='color:{PrimaryColor};'>approved</strong>. You can now set yourself as available and start accepting ride requests.</p>
        ");
        return (subject, body);
    }

    public static (string Subject, string Body) DriverApplicationRejected(string fullName, string reason)
    {
        var subject = "Your driver application was not approved";
        var body = Layout(subject, $@"
            <h2 style='margin:0 0 12px; color:{DarkText};'>Hi {fullName},</h2>
            <p style='margin:0 0 8px; color:{DarkText}; font-size:15px;'>Unfortunately, your driver application was not approved at this time.</p>
            <p style='margin:0; padding:12px; background-color:#FFF7ED; border-left:3px solid {AccentColor}; color:{DarkText}; font-size:14px;'><strong>Reason:</strong> {reason}</p>
        ");
        return (subject, body);
    }

    public static (string Subject, string Body) RideAcceptedByDriver(string passengerName, string driverName, string vehicleInfo, string plateNumber, string rideReference)
    {
        var subject = $"A driver is on the way — Ride {rideReference}";
        var body = Layout(subject, $@"
            <h2 style='margin:0 0 12px; color:{DarkText};'>Hi {passengerName},</h2>
            <p style='margin:0 0 12px; color:{DarkText}; font-size:15px;'><strong>{driverName}</strong> has accepted your ride request and is heading your way.</p>
            <table style='width:100%; font-size:14px; color:{DarkText}; border-collapse:collapse;'>
              <tr><td style='padding:6px 0; color:{MutedText};'>Ride reference</td><td style='padding:6px 0; text-align:right; font-weight:600;'>{rideReference}</td></tr>
              <tr><td style='padding:6px 0; color:{MutedText};'>Vehicle</td><td style='padding:6px 0; text-align:right;'>{vehicleInfo}</td></tr>
              <tr><td style='padding:6px 0; color:{MutedText};'>Plate number</td><td style='padding:6px 0; text-align:right; font-weight:600;'>{plateNumber}</td></tr>
            </table>
        ");
        return (subject, body);
    }

    public static (string Subject, string Body) DriverArrived(string passengerName, string rideReference)
    {
        var subject = $"Your driver has arrived — Ride {rideReference}";
        var body = Layout(subject, $@"
            <h2 style='margin:0 0 12px; color:{DarkText};'>Hi {passengerName},</h2>
            <p style='margin:0; color:{DarkText}; font-size:15px;'>Your driver has arrived at the pickup location. Please head out when you're ready.</p>
        ");
        return (subject, body);
    }

    public static (string Subject, string Body) RideCancelled(string recipientName, string rideReference, string reason)
    {
        var subject = $"Ride cancelled — {rideReference}";
        var body = Layout(subject, $@"
            <h2 style='margin:0 0 12px; color:{DarkText};'>Hi {recipientName},</h2>
            <p style='margin:0 0 8px; color:{DarkText}; font-size:15px;'>Ride <strong>{rideReference}</strong> has been cancelled.</p>
            <p style='margin:0; padding:12px; background-color:#FEF2F2; border-left:3px solid #DC2626; color:{DarkText}; font-size:14px;'><strong>Reason:</strong> {reason}</p>
        ");
        return (subject, body);
    }

    public static (string Subject, string Body) RideCompleted(string passengerName, string rideReference, decimal? fare)
    {
        var subject = $"Ride completed — {rideReference}";
        var fareLine = fare.HasValue ? $"<tr><td style='padding:6px 0; color:{MutedText};'>Fare</td><td style='padding:6px 0; text-align:right; font-weight:600;'>₦{fare.Value:N2}</td></tr>" : "";
        var body = Layout(subject, $@"
            <h2 style='margin:0 0 12px; color:{DarkText};'>Hi {passengerName},</h2>
            <p style='margin:0 0 12px; color:{DarkText}; font-size:15px;'>Your ride has been completed. We hope you had a great trip!</p>
            <table style='width:100%; font-size:14px; color:{DarkText}; border-collapse:collapse;'>
              <tr><td style='padding:6px 0; color:{MutedText};'>Ride reference</td><td style='padding:6px 0; text-align:right; font-weight:600;'>{rideReference}</td></tr>
              {fareLine}
            </table>
        ", "Thank you for riding with us.");
        return (subject, body);
    }
}
