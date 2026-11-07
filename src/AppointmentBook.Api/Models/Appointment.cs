namespace AppointmentBook.Api.Models;

/// <summary>A time slot booked by a customer.</summary>
public sealed class Appointment
{
    public int Id { get; set; }

    /// <summary>Start of the appointment in UTC.</summary>
    public DateTime StartUtc { get; set; }

    /// <summary>End of the appointment in UTC.</summary>
    public DateTime EndUtc { get; set; }

    public required string CustomerName { get; set; }

    public required string CustomerEmail { get; set; }

    public string? CustomerPhone { get; set; }

    public string? Notes { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
