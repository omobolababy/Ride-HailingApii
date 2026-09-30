namespace Ride_HailingApi.Services.Interface;

public interface ISmsService
{
    Task<bool> SendSmsAsync(int userId, string toPhoneNumber, string message);
}