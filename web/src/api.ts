const apiBaseUrl: string = import.meta.env.VITE_API_URL ?? 'http://localhost:5080'

export interface BookingSettings {
  businessName: string
  timeZoneId: string
  slotMinutes: number
  bookingWindowDays: number
}

export interface Slot {
  startUtc: string
  endUtc: string
  isAvailable: boolean
}

export interface BookingRequest {
  startUtc: string
  customerName: string
  customerEmail: string
  customerPhone?: string
  notes?: string
}

export interface BookingConfirmation {
  id: number
  startUtc: string
  endUtc: string
}

export interface AdminAppointment {
  id: number
  startUtc: string
  endUtc: string
  customerName: string
  customerEmail: string
  customerPhone: string | null
  notes: string | null
  status: 'Scheduled' | 'Completed' | 'Cancelled'
}

/** Error carrying the server's message so the UI can show it. */
export class ApiError extends Error {
  readonly status: number

  constructor(message: string, status: number) {
    super(message)
    this.status = status
  }
}

async function request<TResponse>(path: string, init?: RequestInit): Promise<TResponse> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...init,
    headers: { 'Content-Type': 'application/json', ...init?.headers },
  })
  if (!response.ok) {
    let message = `Request failed (${response.status})`
    try {
      const body = await response.json()
      if (typeof body?.error === 'string') message = body.error
      else if (body?.errors) message = Object.values<string[]>(body.errors).flat().join(' ')
    } catch {
      // Body was not JSON; keep the generic message.
    }
    throw new ApiError(message, response.status)
  }
  return response.status === 204 ? (undefined as TResponse) : ((await response.json()) as TResponse)
}

export const bookingApi = {
  getSettings: () => request<BookingSettings>('/api/booking/settings'),
  getSlots: (date: string) => request<Slot[]>(`/api/booking/slots?date=${date}`),
  book: (booking: BookingRequest) =>
    request<BookingConfirmation>('/api/booking/appointments', { method: 'POST', body: JSON.stringify(booking) }),
}

export const adminApi = {
  list: (adminKey: string, includePast: boolean) =>
    request<AdminAppointment[]>(`/api/admin/appointments?includePast=${includePast}`, {
      headers: { 'X-Admin-Key': adminKey },
    }),
  cancel: (adminKey: string, id: number) =>
    request<void>(`/api/admin/appointments/${id}/cancel`, { method: 'POST', headers: { 'X-Admin-Key': adminKey } }),
}
