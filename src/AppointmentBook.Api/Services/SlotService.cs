using AppointmentBook.Api.Data;
using AppointmentBook.Api.Dtos;
using AppointmentBook.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AppointmentBook.Api.Services;

/// <summary>Generates the slot grid for a day and marks which slots are still free.</summary>
public sealed class SlotService(AppointmentDbContext dbContext, IOptions<BookingOptions> options, TimeProvider timeProvider)
{
    private readonly BookingOptions _options = options.Value;

    private TimeZoneInfo BusinessTimeZone => TimeZoneInfo.FindSystemTimeZoneById(_options.TimeZoneId);

    /// <summary>Returns every slot on the given business-local date, in start order.</summary>
    public async Task<IReadOnlyList<SlotDto>> GetSlotsAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var lastBookableDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, BusinessTimeZone)).AddDays(_options.BookingWindowDays);
        if (!_options.WorkDays.Contains(date.DayOfWeek) || date > lastBookableDate)
        {
            return [];
        }

        var slotLength = TimeSpan.FromMinutes(_options.SlotMinutes);
        var candidateSlots = new List<(DateTime StartUtc, DateTime EndUtc)>();
        for (var localStart = date.ToDateTime(_options.OpenTime);
             localStart.Add(slotLength) <= date.ToDateTime(_options.CloseTime);
             localStart = localStart.Add(slotLength))
        {
            // Skip wall-clock times that do not exist (daylight-saving spring-forward gap).
            if (BusinessTimeZone.IsInvalidTime(localStart))
            {
                continue;
            }

            var startUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, BusinessTimeZone);
            candidateSlots.Add((startUtc, startUtc.Add(slotLength)));
        }

        if (candidateSlots.Count == 0)
        {
            return [];
        }

        var windowStartUtc = candidateSlots.Min(slot => slot.StartUtc);
        var windowEndUtc = candidateSlots.Max(slot => slot.EndUtc);
        var busyIntervals = await dbContext.Appointments
            .AsNoTracking()
            .Where(appointment => appointment.Status != AppointmentStatus.Cancelled
                && appointment.StartUtc < windowEndUtc
                && appointment.EndUtc > windowStartUtc)
            .Select(appointment => new { appointment.StartUtc, appointment.EndUtc })
            .ToListAsync(cancellationToken);

        return candidateSlots
            .Select(slot => new SlotDto(
                DateTime.SpecifyKind(slot.StartUtc, DateTimeKind.Utc),
                DateTime.SpecifyKind(slot.EndUtc, DateTimeKind.Utc),
                slot.StartUtc > nowUtc
                    && !busyIntervals.Any(busy => busy.StartUtc < slot.EndUtc && busy.EndUtc > slot.StartUtc)))
            .ToList();
    }

    /// <summary>Finds the slot that starts exactly at <paramref name="startUtc"/>, or null if none exists.</summary>
    public async Task<SlotDto?> FindSlotAsync(DateTime startUtc, CancellationToken cancellationToken)
    {
        var utcStart = DateTime.SpecifyKind(startUtc.ToUniversalTime(), DateTimeKind.Utc);
        var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utcStart, BusinessTimeZone));
        var slots = await GetSlotsAsync(localDate, cancellationToken);
        return slots.FirstOrDefault(slot => slot.StartUtc == utcStart);
    }
}
