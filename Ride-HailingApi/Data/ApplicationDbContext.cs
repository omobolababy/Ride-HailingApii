using Microsoft.EntityFrameworkCore;
using Ride_HailingApi.Entities;


namespace RideHailingApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<DriverProfile> DriverProfiles => Set<DriverProfile>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<EmailOtp> EmailOtps => Set<EmailOtp>();
    public DbSet<PhoneOtp> PhoneOtps => Set<PhoneOtp>();
    public DbSet<PasswordResetOtp> PasswordResetOtps => Set<PasswordResetOtp>();
    public DbSet<Ride> Rides => Set<Ride>();
    public DbSet<RideStatusHistory> RideStatusHistories => Set<RideStatusHistory>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLog => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---------- User ----------
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.PhoneNumber).IsUnique();
            entity.Property(u => u.FullName).HasMaxLength(150).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(200).IsRequired();
            entity.Property(u => u.PhoneNumber).HasMaxLength(20).IsRequired();
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
            
            // Seed a user for testing if needed, or just log all users
            
            entity.HasOne(u => u.DriverProfile)
                  .WithOne(d => d.User)
                  .HasForeignKey<DriverProfile>(d => d.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(u => u.RidesAsPassenger)
                  .WithOne(r => r.Passenger)
                  .HasForeignKey(r => r.PassengerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- DriverProfile ----------
        modelBuilder.Entity<DriverProfile>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.HasIndex(d => d.UserId).IsUnique();
            entity.HasIndex(d => d.LicenseNumber).IsUnique();
            entity.Property(d => d.LicenseNumber).HasMaxLength(50).IsRequired();
            entity.Property(d => d.ApprovalStatus).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(d => d.Vehicle)
                  .WithOne(v => v.DriverProfile)
                  .HasForeignKey<Vehicle>(v => v.DriverProfileId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(d => d.RidesAsDriver)
                  .WithOne(r => r.DriverProfile)
                  .HasForeignKey(r => r.DriverProfileId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- Vehicle ----------
        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(v => v.Id);
            entity.HasIndex(v => v.PlateNumber).IsUnique();
            entity.Property(v => v.PlateNumber).HasMaxLength(20).IsRequired();
            entity.Property(v => v.Make).HasMaxLength(50).IsRequired();
            entity.Property(v => v.Model).HasMaxLength(50).IsRequired();
            entity.Property(v => v.Color).HasMaxLength(30).IsRequired();
        });

        // ---------- EmailOtp ----------
        modelBuilder.Entity<EmailOtp>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.HasIndex(o => new { o.UserId, o.Purpose, o.IsUsed });
            entity.Property(o => o.Code).HasMaxLength(6).IsRequired();
            entity.Property(o => o.Purpose).HasConversion<string>().HasMaxLength(30);

            entity.HasOne(o => o.User)
                  .WithMany(u => u.EmailOtps)
                  .HasForeignKey(o => o.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- PhoneOtp ----------
        modelBuilder.Entity<PhoneOtp>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.HasIndex(o => new { o.UserId, o.IsUsed });
            entity.Property(o => o.Code).HasMaxLength(6).IsRequired();

            entity.HasOne(o => o.User)
                  .WithMany(u => u.PhoneOtps)
                  .HasForeignKey(o => o.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- PasswordResetOtp ----------
        modelBuilder.Entity<PasswordResetOtp>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.HasIndex(o => new { o.UserId, o.IsUsed });
            entity.Property(o => o.Code).HasMaxLength(6).IsRequired();

            entity.HasOne(o => o.User)
                  .WithMany(u => u.PasswordResetOtps)
                  .HasForeignKey(o => o.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- Ride ----------
        modelBuilder.Entity<Ride>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => r.RideReference).IsUnique();
            entity.Property(r => r.RideReference).HasMaxLength(20).IsRequired();
            entity.Property(r => r.PickupLocation).HasMaxLength(300).IsRequired();
            entity.Property(r => r.Destination).HasMaxLength(300).IsRequired();
            entity.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(r => r.EstimatedFare).HasColumnType("decimal(10,2)");
            entity.Property(r => r.RowVersion).IsRowVersion();

            entity.HasOne(r => r.Passenger)
                  .WithMany(u => u.RidesAsPassenger)
                  .HasForeignKey(r => r.PassengerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.DriverProfile)
                  .WithMany(d => d.RidesAsDriver)
                  .HasForeignKey(r => r.DriverProfileId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- RideStatusHistory ----------
        modelBuilder.Entity<RideStatusHistory>(entity =>
        {
            entity.HasKey(h => h.Id);
            entity.HasIndex(h => h.RideId);
            entity.Property(h => h.FromStatus).HasConversion<string>().HasMaxLength(20);
            entity.Property(h => h.ToStatus).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(h => h.Ride)
                  .WithMany(r => r.StatusHistories)
                  .HasForeignKey(h => h.RideId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- Notification ----------
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(n => n.Id);
            entity.HasIndex(n => n.UserId);
            entity.Property(n => n.Type).HasConversion<string>().HasMaxLength(20);
            entity.Property(n => n.Subject).HasMaxLength(200);

            entity.HasOne(n => n.User)
                  .WithMany(u => u.Notifications)
                  .HasForeignKey(n => n.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- AuditLog ----------
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => a.CreatedAtUtc);
            entity.Property(a => a.Action).HasMaxLength(100).IsRequired();
            entity.Property(a => a.ActorRole).HasMaxLength(20);
            entity.Property(a => a.EntityType).HasMaxLength(50);
            entity.Property(a => a.EntityId).HasMaxLength(50);
        });
    }
}
