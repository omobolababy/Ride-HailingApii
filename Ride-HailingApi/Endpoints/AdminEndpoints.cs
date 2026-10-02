using System.Security.Claims;
using Ride_HailingApi.DTOs.Admin;
using Ride_HailingApi.DTOs.Driver;
using Ride_HailingApi.DTOs.Ride;
using Ride_HailingApi.Enums;
using Ride_HailingApi.Helpers;
using Ride_HailingApi.Services.Interface;
using RideHailingApi.DTOs.Common;

namespace Ride_HailingApi.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("Admin")
            .RequireAuthorization(p => p.RequireRole(nameof(UserRole.Admin)));

        group.MapGet("/users", async (string? role, IAdminService svc) =>
            (await svc.GetAllUsersAsync(role)).ToResult())
            .WithName("GetAllUsers");

        group.MapPut("/users/{id:int}/active-status",
            async (int id, SetActiveStatusRequest request, ClaimsPrincipal user, IAdminService svc) =>
                (await svc.SetUserActiveStatusAsync(id, request.IsActive, user.GetUserId())).ToResult())
            .WithValidation<SetActiveStatusRequest>()
            .WithName("SetUserActiveStatus");

        group.MapGet("/drivers/pending", async (IAdminService svc) =>
            (await svc.GetPendingDriversAsync()).ToResult())
            .WithName("GetPendingDrivers");

        group.MapPut("/drivers/{id:int}/approve", async (int id, ClaimsPrincipal user, IAdminService svc) =>
            (await svc.ApproveDriverAsync(id, user.GetUserId())).ToResult())
            .WithName("ApproveDriver");

        group.MapPut("/drivers/{id:int}/reject",
            async (int id, DriverRejectRequest request, ClaimsPrincipal user, IAdminService svc) =>
                (await svc.RejectDriverAsync(id, request, user.GetUserId())).ToResult())
            .WithValidation<DriverRejectRequest>()
            .WithName("RejectDriver");

        group.MapGet("/rides", async (IRideService svc) =>
            (await svc.GetAllRidesAsync()).ToResult())
            .WithName("GetAllRidesForAdmin");

        group.MapGet("/audit-logs", async (IAdminService svc, int pageNumber = 1, int pageSize = 50) =>
            (await svc.GetAuditLogsAsync(pageNumber, pageSize)).ToResult())
            .WithName("GetAuditLogs");

        return app;
    }
}
