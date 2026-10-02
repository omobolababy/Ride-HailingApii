namespace Ride_HailingApi.Helpers;

/// <summary>
/// Named HTTP status codes used across ApiResponse&lt;T&gt; results, so services state
/// intent ("this is a conflict", "this is forbidden") instead of scattering raw numbers.
/// </summary>
public static class ResponseCodes
{
    public const int Success = 200;
    public const int Created = 201;
    public const int BadRequest = 400;
    public const int Unauthorized = 401;
    public const int Forbidden = 403;
    public const int NotFound = 404;
    public const int Conflict = 409;
    public const int ServerError = 500;
}
