namespace AppointmentBook.Api.Services;

/// <summary>Availability rules, bound from the "Booking" configuration section.</summary>
public sealed class BookingOptions
{
    public const string SectionName = "Booking";

    public string BusinessName { get; set; } = "Appointment Book";

    /// <summary>IANA or Windows time zone id in which working hours are expressed.</summary>
    public string TimeZoneId { get; set; } = "UTC";

    public List<DayOfWeek> WorkDays { get; set; } = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];

    public TimeOnly OpenTime { get; set; } = new(9, 0);

    public TimeOnly CloseTime { get; set; } = new(17, 0);

    public int SlotMinutes { get; set; } = 30;

    /// <summary>How many days ahead customers may book.</summary>
    public int BookingWindowDays { get; set; } = 30;
}
