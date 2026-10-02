using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Ride_HailingApi.Helpers;
using Ride_HailingApi.Repositories.Implementation;
using Ride_HailingApi.Repositories.Interface;
using Ride_HailingApi.Services.Implementation;
using Ride_HailingApi.Services.Interface;
using RideHailingApi.Data;
using RideHailingApi.DTOs.Common;
using RideHailingApi.Services.Implementations;

namespace Ride_HailingApi.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>Binds every strongly-typed settings class to its configuration section.</summary>
    public static IServiceCollection AddAppSettings(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<JwtSettings>(config.GetSection("JWT"));
        services.Configure<SmtpMail>(config.GetSection("Email:SmtpMail"));
        // BaseUrl / ApiKey / Subject live under SmsSettings:TelecomeAbode (binding the parent section left them empty,
        // which silently disabled every SMS).
        services.Configure<SmsSettings>(config.GetSection("SmsSettings:TelecomeAbode"));
        services.Configure<OtpSettings>(config.GetSection("SmsSettings:OtpSettings"));
        return services;
    }

    public static IServiceCollection AddAppDatabase(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(config.GetConnectionString("DefaultConnection")));
        return services;
    }

    public static IServiceCollection AddAppAuthentication(this IServiceCollection services, IConfiguration config)
    {
        var jwtKey = config["JWT:Key"] ?? throw new InvalidOperationException("JWT:Key is missing.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = config["JWT:Issuer"],
                    ValidAudience = config["JWT:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ClockSkew = TimeSpan.Zero
                };

                // Authentication/authorization failures happen before any endpoint runs, so give them the
                // same ApiResponse body (and ResponseCode) as every other response.
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        await WriteApiResponseAsync(context.HttpContext, ResponseCodes.Unauthorized,
                            "Authentication is required. Provide a valid Bearer token.");
                    },
                    OnForbidden = context => WriteApiResponseAsync(context.HttpContext, ResponseCodes.Forbidden,
                        "You do not have permission to access this resource.")
                };
            });

        services.AddAuthorization();
        return services;
    }

    private static Task WriteApiResponseAsync(HttpContext httpContext, int responseCode, string message)
    {
        httpContext.Response.StatusCode = responseCode;
        return httpContext.Response.WriteAsJsonAsync(ApiResponse<string>.FailResponse(message, responseCode));
    }

    public static IServiceCollection AddAppSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Ride-Hailing API",
                Version = "v1",
                Description = "Student Ride-Hailing REST API (minimal APIs)"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter: Bearer {your JWT token}"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });
        });
        return services;
    }

    public static IServiceCollection AddAppRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDriverRepository, DriverRepository>();
        services.AddScoped<IRideRepository, RideRepository>();
        services.AddScoped<IOtpRepository, OtpRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        return services;
    }

    public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPassengerService, PassengerService>();
        services.AddScoped<IDriverService, DriverService>();
        services.AddScoped<IRideService, RideService>();
        services.AddScoped<IAdminService, AdminService>();

        services.AddSmsProvider(config);
        return services;
    }

    private static IServiceCollection AddSmsProvider(this IServiceCollection services, IConfiguration config)
    {
        var provider = config["SmsSettings:Provider"];

        if (string.Equals(provider, "TelecomeAbode", StringComparison.OrdinalIgnoreCase))
        {
            // AddHttpClient<TInterface, TImpl> registers ISmsService itself (transient) with a typed HttpClient.
            services.AddHttpClient<ISmsService, TelecomeAbodeService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(60);
            });
            return services;
        }

        throw new InvalidOperationException(
            $"Unsupported or missing SmsSettings:Provider '{provider}'. Supported: TelecomeAbode.");
    }
}
