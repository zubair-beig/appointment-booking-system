using System.ComponentModel.DataAnnotations;
using AppointmentBook.Api.Models;

namespace AppointmentBook.Api.Dtos;

/// <summary>Public booking configuration the UI needs to render the picker.</summary>
public sealed record BookingSettingsDto(
    string BusinessName,
    string TimeZoneId,
    int SlotMinutes,
    int BookingWindowDays);

/// <summary>One bookable slot on a given day.</summary>
public sealed record SlotDto(DateTime StartUtc, DateTime EndUtc, bool IsAvailable);

/// <summary>Payload a customer submits to book a slot.</summary>
public sealed record BookingRequest(
    DateTime StartUtc,
    [Required, StringLength(200)] string CustomerName,
    [Required, EmailAddress, StringLength(254)] string CustomerEmail,
    [StringLength(40)] string? CustomerPhone,
    [StringLength(1000)] string? Notes);

/// <summary>Appointment as shown to the owner.</summary>
public sealed record AppointmentDto(
    int Id,
    DateTime StartUtc,
    DateTime EndUtc,
    string CustomerName,
    string CustomerEmail,
    string? CustomerPhone,
    string? Notes,
    AppointmentStatus Status);

/// <summary>Confirmation returned to the customer after booking.</summary>
public sealed record BookingConfirmationDto(int Id, DateTime StartUtc, DateTime EndUtc);
