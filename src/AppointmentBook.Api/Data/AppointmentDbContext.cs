using AppointmentBook.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AppointmentBook.Api.Data;

/// <summary>EF Core context for the appointment book.</summary>
public sealed class AppointmentDbContext(DbContextOptions<AppointmentDbContext> options) : DbContext(options)
{
    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.Property(appointment => appointment.CustomerName).HasMaxLength(200).IsRequired();
            entity.Property(appointment => appointment.CustomerEmail).HasMaxLength(254).IsRequired();
            entity.Property(appointment => appointment.CustomerPhone).HasMaxLength(40);
            entity.Property(appointment => appointment.Notes).HasMaxLength(1000);
            entity.Property(appointment => appointment.Status).HasConversion<string>().HasMaxLength(20);

            // Database-level guard against two people grabbing the same slot at the same moment.
            entity.HasIndex(appointment => appointment.StartUtc)
                .IsUnique()
                .HasFilter("\"Status\" <> 'Cancelled'");
        });
    }
}
