using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Crypto.Generators;
using Ride_HailingApi.Entities;
using Ride_HailingApi.Enums;


namespace RideHailingApi.Data;

/// <summary>
/// Applies EF Core migrations (or creates the schema if none exist yet) and seeds the first Admin
/// account, since Admins cannot self-register. Credentials come from the "SeedAdmin" config section.
/// </summary>
public static class SeedData
{
    public static async Task InitialiseAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        if (db.Database.GetMigrations().Any())
            await db.Database.MigrateAsync();
        else
            await db.Database.EnsureCreatedAsync();

        var section = config.GetSection("SeedAdmin");
        var email = section["Email"]?.Trim().ToLowerInvariant();
        var password = section["Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return;

        if (await db.Users.AnyAsync(u => u.Role == UserRole.Admin)) return;

        db.Users.Add(new User
        {
            FullName = section["FullName"] ?? "System Admin",
            Email = email,
            PhoneNumber = section["PhoneNumber"] ?? section["Phone"] ?? "+2340000000000",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = UserRole.Admin,
            IsEmailVerified = true,
            IsPhoneVerified = true,
            IsActive = true
        });
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded default admin account: {Email}", email);
    }
}