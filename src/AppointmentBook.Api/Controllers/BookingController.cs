using AppointmentBook.Api.Data;
using AppointmentBook.Api.Dtos;
using AppointmentBook.Api.Models;
using AppointmentBook.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AppointmentBook.Api.Controllers;

/// <summary>Public endpoints used by customers to see availability and book a slot.</summary>
[ApiController]
[Route("api/booking")]
public sealed class BookingController(
    AppointmentDbContext dbContext,
    SlotService slotService,
    IOptions<BookingOptions> options) : ControllerBase
{
    [HttpGet("settings")]
    public BookingSettingsDto GetSettings()
    {
        var bookingOptions = options.Value;
        return new BookingSettingsDto(
            bookingOptions.BusinessName,
            bookingOptions.TimeZoneId,
            bookingOptions.SlotMinutes,
            bookingOptions.BookingWindowDays);
    }

    /// <param name="date">Business-local date, formatted yyyy-MM-dd.</param>
    [HttpGet("slots")]
    public async Task<IReadOnlyList<SlotDto>> GetSlotsAsync([FromQuery] DateOnly date, CancellationToken cancellationToken)
    {
        return await slotService.GetSlotsAsync(date, cancellationToken);
    }

    [HttpPost("appointments")]
    public async Task<ActionResult<BookingConfirmationDto>> BookAsync(BookingRequest request, CancellationToken cancellationToken)
    {
        var slot = await slotService.FindSlotAsync(request.StartUtc, cancellationToken);
        if (slot is null)
        {
            return BadRequest(new { error = "That time is not a valid slot." });
        }

        if (!slot.IsAvailable)
        {
            return Conflict(new { error = "Sorry, that slot has just been taken. Please pick another." });
        }

        var appointment = new Appointment
        {
            StartUtc = slot.StartUtc,
            EndUtc = slot.EndUtc,
            CustomerName = request.CustomerName.Trim(),
            CustomerEmail = request.CustomerEmail.Trim(),
            CustomerPhone = request.CustomerPhone?.Trim(),
            Notes = request.Notes?.Trim(),
        };

        dbContext.Appointments.Add(appointment);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Lost a race: the filtered unique index on StartUtc rejected the duplicate booking.
            return Conflict(new { error = "Sorry, that slot has just been taken. Please pick another." });
        }

        return Created(
            $"/api/booking/appointments/{appointment.Id}",
            new BookingConfirmationDto(appointment.Id, slot.StartUtc, slot.EndUtc));
    }
}
