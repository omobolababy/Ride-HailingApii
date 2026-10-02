using RideHailingApi.Data;
using RideHailingApi.Middleware;

namespace Ride_HailingApi.Extensions;

public static class WebApplicationExtensions
{
    /// <summary>Request pipeline, in the order it must run.</summary>
    public static WebApplication UseAppPipeline(this WebApplication app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }

    /// <summary>Applies migrations and seeds the first admin account.</summary>
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        await SeedData.InitialiseAsync(scope.ServiceProvider);
    }
}
