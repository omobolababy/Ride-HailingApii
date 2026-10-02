namespace Ride_HailingApi.Endpoints;

public static class EndpointRouteBuilderExtensions
{
    /// <summary>Maps every feature's minimal-API endpoints.</summary>
    public static IEndpointRouteBuilder MapAppEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapAuthEndpoints()
            .MapPassengerEndpoints()
           .MapDriverEndpoints()
           .MapRideEndpoints()
           .MapAdminEndpoints();
        return app;
    }
}
