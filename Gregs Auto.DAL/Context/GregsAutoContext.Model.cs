using Gregs_Auto.Domain.EntityModels;
using Microsoft.EntityFrameworkCore;

namespace Gregs_Auto.DAL.Context;

// Model configuration for the hand-written entities, kept out of the scaffolded
// GregsAutoContext.cs so regenerating it doesn't drop them. OnModelCreatingPartial
// is the hook the generated OnModelCreating calls last.
public partial class GregsAutoContext
{
    public virtual DbSet<LoginAudit> LoginAudits { get; set; }

    public virtual DbSet<BookingRequest> BookingRequests { get; set; }

    public virtual DbSet<Shop> Shops { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Shop>(entity =>
        {
            entity.ToTable("Shops");
            entity.HasKey(e => e.ShopId).HasName("PK_Shops");

            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(30);
            entity.Property(e => e.AddressLine).HasMaxLength(200);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.State).HasMaxLength(50);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.TimeZoneId).IsRequired().HasMaxLength(100);

            entity.Property(e => e.ClosedDaysRaw)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("ClosedDays");

            entity.Property(e => e.TierName)
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnName("Tier");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");

            // Computed in C# from ClosedDaysRaw / TierName — not columns.
            entity.Ignore(e => e.ClosedDays);
            entity.Ignore(e => e.Tier);
        });

        modelBuilder.Entity<BookingRequest>(entity =>
        {
            entity.HasKey(e => e.BookingRequestId).HasName("PK_BookingRequests");

            entity.Property(e => e.CustomerName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Phone).IsRequired().HasMaxLength(30);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.VehicleMake).IsRequired().HasMaxLength(50);
            entity.Property(e => e.VehicleModel).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue(BookingRequestStatus.Pending)
                .HasAnnotation("Relational:DefaultConstraintName", "DF_BookingRequests_Status");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasAnnotation("Relational:DefaultConstraintName", "DF_BookingRequests_CreatedAt");

            // VehicleDescription is computed in C#, not a column.
            entity.Ignore(e => e.VehicleDescription);

            entity.HasIndex(e => new { e.Status, e.CreatedAt }, "IX_BookingRequests_Status_CreatedAt");

            entity.HasOne(d => d.Service).WithMany()
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_BookingRequests_Services");

            entity.HasOne(d => d.HandledByUser).WithMany()
                .HasForeignKey(d => d.HandledByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_BookingRequests_Users");

            entity.HasOne(d => d.Appointment).WithMany()
                .HasForeignKey(d => d.AppointmentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_BookingRequests_Appointments");
        });

        modelBuilder.Entity<LoginAudit>(entity =>
        {
            entity.ToTable("LoginAudit");

            entity.HasKey(e => e.LoginAuditId).HasName("PK_LoginAudit");

            entity.Property(e => e.EmailAttempted)
                .IsRequired()
                .HasMaxLength(255);
            entity.Property(e => e.Event)
                .IsRequired()
                .HasMaxLength(20);
            entity.Property(e => e.IpAddress)
                .HasMaxLength(45);
            entity.Property(e => e.OccurredAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasAnnotation("Relational:DefaultConstraintName", "DF_LoginAudit_OccurredAt");

            entity.HasIndex(e => e.OccurredAt, "IX_LoginAudit_OccurredAt").IsDescending();
            entity.HasIndex(e => e.EmailAttempted, "IX_LoginAudit_EmailAttempted");

            // SetNull, not Cascade: deactivating or removing a user must not
            // erase the record of what happened.
            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_LoginAudit_Users");
        });

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");

            // Computed in C# from ScheduledAt + DurationMinutes.
            entity.Ignore(e => e.EndsAt);
        });

        modelBuilder.Entity<Service>(entity => entity.Property(e => e.IsActive)
            .HasDefaultValue(true)
            .HasAnnotation("Relational:DefaultConstraintName", "DF_Services_IsActive"));

        modelBuilder.Entity<Customer>(entity => entity.Property(e => e.IsActive)
            .HasDefaultValue(true)
            .HasAnnotation("Relational:DefaultConstraintName", "DF_Customers_IsActive"));

        modelBuilder.Entity<Vehicle>(entity => entity.Property(e => e.IsActive)
            .HasDefaultValue(true)
            .HasAnnotation("Relational:DefaultConstraintName", "DF_Vehicles_IsActive"));

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasAnnotation("Relational:DefaultConstraintName", "DF_Users_IsActive");
            entity.Property(e => e.FailedLoginCount)
                .HasDefaultValue(0)
                .HasAnnotation("Relational:DefaultConstraintName", "DF_Users_FailedLoginCount");
        });
    }
}
