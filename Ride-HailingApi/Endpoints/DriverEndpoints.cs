using System.Security.Claims;
using Ride_HailingApi.DTOs.Driver;
using Ride_HailingApi.DTOs.Ride;
using Ride_HailingApi.Enums;
using Ride_HailingApi.Helpers;
using Ride_HailingApi.Services.Interface;
using RideHailingApi.DTOs.Common;

namespace Ride_HailingApi.Endpoints;

public static class DriverEndpoints
{
    public static IEndpointRouteBuilder MapDriverEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/driver")
            .WithTags("Driver")
            .RequireAuthorization(p => p.RequireRole(nameof(UserRole.Driver)));

        group.MapPost("/onboard", async (DriverOnboardRequest request, ClaimsPrincipal user, IDriverService svc) =>
            (await svc.OnboardAsync(user.GetUserId(), request)).ToResult())
            .WithValidation<DriverOnboardRequest>()
            .WithName("OnboardDriver")
            .Produces<ApiResponse<DriverResponse>>(ResponseCodes.Created)
            .Produces<ApiResponse<DriverResponse>>(ResponseCodes.Forbidden)
            .Produces<ApiResponse<DriverResponse>>(ResponseCodes.Conflict);

        group.MapGet("/profile", async (ClaimsPrincipal user, IDriverService svc) =>
            (await svc.GetMyDriverProfileAsync(user.GetUserId())).ToResult())
            .WithName("GetMyDriverProfile")
            .Produces<ApiResponse<DriverResponse>>(ResponseCodes.Success)
            .Produces<ApiResponse<DriverResponse>>(ResponseCodes.NotFound);

        group.MapPut("/availability", async (DriverAvailabilityRequest request, ClaimsPrincipal user, IDriverService svc) =>
            (await svc.SetAvailabilityAsync(user.GetUserId(), request.IsAvailable)).ToResult())
            .WithValidation<DriverAvailabilityRequest>()
            .WithName("SetDriverAvailability")
            .Produces<ApiResponse<DriverResponse>>(ResponseCodes.Success)
            .Produces<ApiResponse<DriverResponse>>(ResponseCodes.NotFound)
            .Produces<ApiResponse<DriverResponse>>(ResponseCodes.Forbidden);

        group.MapGet("/rides/available", async (ClaimsPrincipal user, IDriverService svc) =>
            (await svc.GetAvailableRideRequestsAsync(user.GetUserId())).ToResult())
            .WithName("GetAvailableRideRequests")
            .Produces<ApiResponse<IEnumerable<RideResponse>>>(ResponseCodes.Success)
            .Produces<ApiResponse<IEnumerable<RideResponse>>>(ResponseCodes.NotFound)
            .Produces<ApiResponse<IEnumerable<RideResponse>>>(ResponseCodes.Forbidden);

        return app;
    }
}
