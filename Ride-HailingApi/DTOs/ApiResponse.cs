using Ride_HailingApi.Helpers;

namespace RideHailingApi.DTOs.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public int ResponseCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResponse<T> SuccessResponse(T data, string message = "Request successful",
        int responseCode = ResponseCodes.Success)
    {
        return new ApiResponse<T> { Success = true, ResponseCode = responseCode, Message = message, Data = data };
    }

    public static ApiResponse<T> FailResponse(string message, int responseCode = ResponseCodes.BadRequest,
        List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false, ResponseCode = responseCode, Message = message, Data = default, Errors = errors
        };
    }
}
