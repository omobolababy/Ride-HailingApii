namespace Ride_HailingApi.Utils;

public static class SmsUtils
{
    public static string PhoneVerificationOtp(string otp, int expiryMinutes) =>
        $"Your Ride-Hailing verification code is {otp}. It expires in {expiryMinutes} minutes. Do not share this code with anyone.";

    public static string PasswordResetOtp(string otp, int expiryMinutes) =>
        $"Your Ride-Hailing password reset code is {otp}. It expires in {expiryMinutes} minutes. If you didn't request this, ignore this message.";

    public static string RideAcceptedByDriver(string driverName, string plateNumber, string rideReference) =>
        $"Ride {rideReference}: {driverName} is on the way ({plateNumber}). Track your ride in the app.";

    public static string DriverArrived(string rideReference) =>
        $"Ride {rideReference}: Your driver has arrived at the pickup point.";

    public static string RideCancelled(string rideReference, string reason) =>
        $"Ride {rideReference} was cancelled. Reason: {reason}.";

    public static string RideCompleted(string rideReference) =>
        $"Ride {rideReference} is complete. Thanks for riding with us!";

    public static string DriverApplicationApproved() =>
        "Good news! Your driver application has been approved. You can now go available and start accepting rides.";

    public static string DriverApplicationRejected(string reason) =>
        $"Your driver application was not approved. Reason: {reason}.";
}
