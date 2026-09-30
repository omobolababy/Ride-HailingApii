using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ride_HailingApi.Helpers;
using Ride_HailingApi.Services.Interface;

namespace Ride_HailingApi.Services.Implementation;

public class TelecomeAbodeService : ISmsService
{
    private readonly HttpClient _httpClient;
    private readonly SmsSettings _telecomeSmsSettings;
    private readonly ILogger<TelecomeAbodeService> _logger;

    public TelecomeAbodeService(HttpClient httpClient, IOptions<SmsSettings> telecomeSmsSettings, ILogger<TelecomeAbodeService> logger)
    {
        _httpClient = httpClient;
        _telecomeSmsSettings = telecomeSmsSettings.Value;
        _logger = logger;
    }

    public async Task<bool> SendSmsAsync(int userId, string toPhoneNumber, string message)
    {
        if (string.IsNullOrWhiteSpace(toPhoneNumber))
        {
            _logger.LogInformation("SMS sending cancelled: Recipient phone number is empty");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_telecomeSmsSettings.BaseUrl) || string.IsNullOrWhiteSpace(_telecomeSmsSettings.ApiKey))
        {
            _logger.LogInformation("TelecomeAbode SMS sending cancelled: BaseUrl or APIKey is empty");
            return false;
        }

        // normalize the recipient phone number 
        var rawNumbers = toPhoneNumber.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        var normalizeList = new List<string>();

        foreach (var num in rawNumbers)
        {
            var cleanPhone = num.Trim();

            // If it starts with "+234", convert it to "0"
            if (cleanPhone.StartsWith("+234"))
            {
                cleanPhone = "0" + cleanPhone.Substring(4);
            }
            else if (cleanPhone.StartsWith("234"))
            {
                cleanPhone = "0" + cleanPhone.Substring(3);
            }
            else if (cleanPhone.StartsWith("+"))
            {
                cleanPhone = cleanPhone.Substring(1);
            }
            normalizeList.Add(cleanPhone);
        }

        if (normalizeList.Count == 0)
        {
            _logger.LogInformation("TelecomeAbode SMS sending cancelled: Recipient phone number is empty after normalization");
            return false;
        }

        var bulkTo = string.Join(",", normalizeList);

        var reqPayload = new Dictionary<string, string>
        {
            { "subject", _telecomeSmsSettings.Subject },
            { "bulkPhones", bulkTo },
            { "message", message }
        };

        try
        {
            var content = new FormUrlEncodedContent(reqPayload);
            var request = new HttpRequestMessage(HttpMethod.Post, _telecomeSmsSettings.BaseUrl)
            {
                Content = content
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _telecomeSmsSettings.ApiKey);
            request.Headers.Add("Accept", "application/json");

            _logger.LogInformation("Sending SMS alert to {BulkTo} via TelecomAbode..", bulkTo);
            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("<====Raw Response From TelecomeAbode====>\n {ResponseContent}", responseContent);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("SMS sent successfully to {BulkTo} via TelecomAbode..", bulkTo);
                return true;
            }

            _logger.LogInformation("Failed to send sms to {BulkTo} via TelecomAbode. Status {StatusCode}..", bulkTo, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while sending SMS via TelecomAbode to {BulkTo}", bulkTo);
            return false;
        }
    }
}