using Ride_HailingApi.Endpoints;
using Ride_HailingApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAppSettings(builder.Configuration)
    .AddAppDatabase(builder.Configuration)
    .AddAppAuthentication(builder.Configuration)
    .AddAppSwagger()
    .AddAppRepositories()
    .AddAppServices(builder.Configuration);

var app = builder.Build();

await app.InitialiseDatabaseAsync();

app.UseAppPipeline();
app.MapAppEndpoints();

app.Run();
