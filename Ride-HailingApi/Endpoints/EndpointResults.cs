using System.ComponentModel.DataAnnotations;
using Ride_HailingApi.Helpers;
using RideHailingApi.DTOs.Common;

namespace Ride_HailingApi.Endpoints;

public static class EndpointResults
{
   
    /// Writes the ApiResponse as JSON using its own ResponseCode as the HTTP status
    /// (200, 201, 400, 401, 403, 404, 409, 500), so the body and the status line always agree.
    
    public static IResult ToResult<T>(this ApiResponse<T> response)
    {
        var status = response.ResponseCode != 0
            ? response.ResponseCode
            : response.Success ? ResponseCodes.Success : ResponseCodes.BadRequest;

        return Results.Json(response, statusCode: status);
    }

    /// <summary>Adds DataAnnotations validation for the request body (minimal APIs do not do this by default).</summary>
    public static RouteHandlerBuilder WithValidation<T>(this RouteHandlerBuilder builder) where T : class =>
        builder.AddEndpointFilter<ValidationFilter<T>>();
}

public sealed class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<T>().FirstOrDefault();
        if (request is null)
            return ApiResponse<string>.FailResponse("Request body is required.", ResponseCodes.BadRequest).ToResult();

        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true))
        {
            var errors = results.Select(r => r.ErrorMessage ?? "Invalid value.").ToList();
            return ApiResponse<string>.FailResponse("Validation failed.", ResponseCodes.BadRequest, errors).ToResult();
        }

        return await next(context);
    }
}
