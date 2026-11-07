using AppointmentBook.Api.Data;
using AppointmentBook.Api.Dtos;
using AppointmentBook.Api.Filters;
using AppointmentBook.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppointmentBook.Api.Controllers;

/// <summary>Owner-only endpoints, protected by the X-Admin-Key header.</summary>
[ApiController]
[Route("api/admin")]
[ServiceFilter<AdminKeyFilter>]
public sealed class AdminController(AppointmentDbContext dbContext, TimeProvider timeProvider) : ControllerBase
{
    /// <summary>Lists appointments from now onwards (or all, when includePast is set), soonest first.</summary>
    [HttpGet("appointments")]
    public async Task<IReadOnlyList<AppointmentDto>> ListAsync([FromQuery] bool includePast, CancellationToken cancellationToken)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var query = dbContext.Appointments.AsNoTracking();
        if (!includePast)
        {
            query = query.Where(appointment => appointment.EndUtc > nowUtc);
        }

        var appointments = await query.OrderBy(appointment => appointment.StartUtc).ToListAsync(cancellationToken);
        return appointments
            .Select(appointment => new AppointmentDto(
                appointment.Id,
                DateTime.SpecifyKind(appointment.StartUtc, DateTimeKind.Utc),
                DateTime.SpecifyKind(appointment.EndUtc, DateTimeKind.Utc),
                appointment.CustomerName,
                appointment.CustomerEmail,
                appointment.CustomerPhone,
                appointment.Notes,
                appointment.Status))
            .ToList();
    }

    [HttpPost("appointments/{id:int}/cancel")]
    public async Task<IActionResult> CancelAsync(int id, CancellationToken cancellationToken)
    {
        var updatedCount = await dbContext.Appointments
            .Where(appointment => appointment.Id == id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(appointment => appointment.Status, AppointmentStatus.Cancelled), cancellationToken);

        return updatedCount == 0 ? NotFound() : NoContent();
    }
}
