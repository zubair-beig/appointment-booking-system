namespace AppointmentBook.Api.Models;

/// <summary>Lifecycle state of an appointment.</summary>
public enum AppointmentStatus
{
    Scheduled = 0,
    Completed = 1,
    Cancelled = 2,
}
