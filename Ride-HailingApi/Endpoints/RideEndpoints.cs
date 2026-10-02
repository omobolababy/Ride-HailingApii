using System.Security.Claims;
using Ride_HailingApi.DTOs.Ride;
using Ride_HailingApi.Enums;
using Ride_HailingApi.Helpers;
using Ride_HailingApi.Services.Interface;
using RideHailingApi.DTOs.Common;

namespace Ride_HailingApi.Endpoints;

public static class RideEndpoints
{
    public static IEndpointRouteBuilder MapRideEndpoints(this IEndpointRouteBuilder app)
    {
        
        var group = app.MapGroup("/api/ride")
            .WithTags("Ride")
            .RequireAuthorization();

        group.MapPost("", async (CreateRideRequest request, ClaimsPrincipal user, IRideService svc) =>
            (await svc.CreateRideAsync(user.GetUserId(), request)).ToResult())
            .WithValidation<CreateRideRequest>()
            .RequireAuthorization(p => p.RequireRole(nameof(UserRole.Passenger)))
            .WithName("CreateRide")
            .Produces<ApiResponse<RideResponse>>(ResponseCodes.Created);

        group.MapGet("/my-rides", async (ClaimsPrincipal user, IRideService svc) =>
            (await svc.GetMyRidesAsync(user.GetUserId())).ToResult())
            .RequireAuthorization(p => p.RequireRole(nameof(UserRole.Passenger)))
            .WithName("GetMyRides");

        group.MapGet("/assigned-to-me", async (ClaimsPrincipal user, IRideService svc) =>
            (await svc.GetRidesForDriverAsync(user.GetUserId())).ToResult())
            .RequireAuthorization(p => p.RequireRole(nameof(UserRole.Driver)))
            .WithName("GetRidesAssignedToMe");

        // Ownership is enforced inside the service- a Passenger may only view their own ride,
        // a Driver only a ride assigned to them. Admins may view any.
        group.MapGet("/{id:int}", async (int id, ClaimsPrincipal user, IRideService svc) =>
        {
            if (!Enum.TryParse<UserRole>(user.GetRole(), out var role))
                return ApiResponse<RideResponse>.FailResponse("Your account role is not recognised.", ResponseCodes.Forbidden).ToResult();

            return (await svc.GetRideByIdAsync(id, user.GetUserId(), role)).ToResult();
        })
        .WithName("GetRideById");

        group.MapPut("/{id:int}/cancel", async (int id, CancelRideRequest request, ClaimsPrincipal user, IRideService svc) =>
            (await svc.CancelRideAsync(id, user.GetUserId(), request)).ToResult())
            .WithValidation<CancelRideRequest>()
            .RequireAuthorization(p => p.RequireRole(nameof(UserRole.Passenger)))
            .WithName("CancelRide");

        group.MapPut("/{id:int}/accept", async (int id, ClaimsPrincipal user, IRideService svc) =>
            (await svc.AcceptRideAsync(id, user.GetUserId())).ToResult())
            .RequireAuthorization(p => p.RequireRole(nameof(UserRole.Driver)))
            .WithName("AcceptRide");

        group.MapPut("/{id:int}/status", async (int id, RideStatusUpdateRequest request, ClaimsPrincipal user, IRideService svc) =>
            (await svc.UpdateRideStatusAsync(id, user.GetUserId(), request)).ToResult())
            .WithValidation<RideStatusUpdateRequest>()
            .RequireAuthorization(p => p.RequireRole(nameof(UserRole.Driver)))
            .WithName("UpdateRideStatus");

        return app;
    }
}
