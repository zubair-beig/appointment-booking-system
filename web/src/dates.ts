/** Today's calendar date (yyyy-MM-dd) in the given time zone. */
export function todayIn(timeZoneId: string): string {
  return new Intl.DateTimeFormat('en-CA', { timeZone: timeZoneId }).format(new Date())
}

/** Adds whole days to a yyyy-MM-dd string without any time-zone drift. */
export function addDays(isoDate: string, days: number): string {
  const [year, month, day] = isoDate.split('-').map(Number)
  const shifted = new Date(Date.UTC(year, month - 1, day + days))
  return shifted.toISOString().slice(0, 10)
}

function asUtcNoon(isoDate: string): Date {
  return new Date(`${isoDate}T12:00:00Z`)
}

export function formatDayChip(isoDate: string): { weekday: string; day: string; month: string } {
  const date = asUtcNoon(isoDate)
  const format = (options: Intl.DateTimeFormatOptions) =>
    new Intl.DateTimeFormat('en-GB', { timeZone: 'UTC', ...options }).format(date)
  return { weekday: format({ weekday: 'short' }), day: format({ day: 'numeric' }), month: format({ month: 'short' }) }
}

export function formatLongDate(isoDate: string): string {
  return new Intl.DateTimeFormat('en-GB', { timeZone: 'UTC', dateStyle: 'full' }).format(asUtcNoon(isoDate))
}

export function formatTime(utcIso: string, timeZoneId: string): string {
  return new Intl.DateTimeFormat('en-GB', { timeZone: timeZoneId, hour: '2-digit', minute: '2-digit' }).format(
    new Date(utcIso),
  )
}

export function formatDateTime(utcIso: string, timeZoneId: string): string {
  return new Intl.DateTimeFormat('en-GB', {
    timeZone: timeZoneId,
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(utcIso))
}
