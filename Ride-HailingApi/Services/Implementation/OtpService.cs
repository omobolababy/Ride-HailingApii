using Microsoft.Extensions.Options;
using Ride_HailingApi.Helpers;
using Ride_HailingApi.Services.Interface;

namespace Ride_HailingApi.Services.Implementation;

public class OtpService : IOtpService
{
    private readonly OtpSettings _otpSettings;

    public OtpService(IOptions<OtpSettings> otpSettings)
    {
        _otpSettings = otpSettings.Value;
    }

    public string GenerateOtpCode()
    {
        var max = (int)Math.Pow(10, _otpSettings.Length) - 1;
        var min = (int)Math.Pow(10, _otpSettings.Length - 1);
        return System.Security.Cryptography.RandomNumberGenerator.GetInt32(min, max + 1).ToString();
    }
}