using System.Security.Claims;
using Ride_HailingApi.DTOs.Passenger;
using Ride_HailingApi.Enums;
using Ride_HailingApi.Helpers;
using Ride_HailingApi.Services.Interface;
using RideHailingApi.DTOs.Common;

namespace Ride_HailingApi.Endpoints;

public static class PassengerEndpoints
{
    public static IEndpointRouteBuilder MapPassengerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/passenger")
            .WithTags("Passenger")
            .RequireAuthorization(p => p.RequireRole(nameof(UserRole.Passenger)));

        group.MapGet("/profile", async (ClaimsPrincipal user, IPassengerService svc) =>
            (await svc.GetProfileAsync(user.GetUserId())).ToResult())
            .WithName("GetPassengerProfile");

        group.MapPut("/profile", async (UpdatePassengerProfileRequest request, ClaimsPrincipal user, IPassengerService svc) =>
            (await svc.UpdateProfileAsync(user.GetUserId(), request)).ToResult())
            .WithValidation<UpdatePassengerProfileRequest>()
            .WithName("UpdatePassengerProfile");

        return app;
    }
}
