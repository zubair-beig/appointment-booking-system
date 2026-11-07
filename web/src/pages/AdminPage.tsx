import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { adminApi, type AdminAppointment, type BookingSettings } from '../api'
import { formatDateTime } from '../dates'

interface AdminPageProps {
  settings: BookingSettings
}

const adminKeyStorageKey = 'appointment-book-admin-key'

const statusStyles: Record<AdminAppointment['status'], string> = {
  Scheduled: 'bg-indigo-50 text-indigo-700',
  Completed: 'bg-emerald-50 text-emerald-700',
  Cancelled: 'bg-slate-100 text-slate-500 line-through',
}

export default function AdminPage({ settings }: AdminPageProps) {
  // Kept in sessionStorage so it is cleared when the tab closes.
  const [adminKey, setAdminKey] = useState(() => sessionStorage.getItem(adminKeyStorageKey) ?? '')
  const [keyInput, setKeyInput] = useState('')
  const [appointments, setAppointments] = useState<AdminAppointment[] | null>(null)
  const [includePast, setIncludePast] = useState(false)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  const load = useCallback(() => {
    if (!adminKey) return
    adminApi
      .list(adminKey, includePast)
      .then((result) => {
        setAppointments(result)
        setErrorMessage(null)
      })
      .catch(() => {
        sessionStorage.removeItem(adminKeyStorageKey)
        setAdminKey('')
        setErrorMessage('That key was not accepted.')
      })
  }, [adminKey, includePast])

  useEffect(load, [load])

  const signIn = (event: FormEvent) => {
    event.preventDefault()
    sessionStorage.setItem(adminKeyStorageKey, keyInput)
    setAdminKey(keyInput)
  }

  if (!adminKey) {
    return (
      <form onSubmit={signIn} className="mx-auto max-w-sm space-y-4 rounded-3xl bg-white p-8 shadow-xl shadow-indigo-100">
        <h1 className="text-xl font-semibold">Owner login</h1>
        <input
          type="password"
          required
          placeholder="Admin key"
          value={keyInput}
          onChange={(e) => setKeyInput(e.target.value)}
          className="w-full rounded-xl border border-slate-200 px-3 py-2.5 text-sm outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100"
        />
        {errorMessage && <p className="text-sm text-rose-600">{errorMessage}</p>}
        <button className="w-full rounded-xl bg-indigo-600 py-3 text-sm font-semibold text-white hover:bg-indigo-700">
          Sign in
        </button>
      </form>
    )
  }

  const cancel = async (id: number) => {
    if (!window.confirm('Cancel this appointment?')) return
    await adminApi.cancel(adminKey, id)
    load()
  }

  return (
    <div>
      <div className="flex items-end justify-between">
        <h1 className="text-3xl font-semibold tracking-tight">Appointments</h1>
        <label className="flex items-center gap-2 text-sm text-slate-500">
          <input type="checkbox" checked={includePast} onChange={(e) => setIncludePast(e.target.checked)} />
          Include past
        </label>
      </div>

      <div className="mt-6 overflow-hidden rounded-3xl bg-white shadow-xl shadow-indigo-100/60">
        {appointments === null ? (
          <p className="p-8 text-slate-400">Loading…</p>
        ) : appointments.length === 0 ? (
          <p className="p-8 text-slate-500">No upcoming appointments.</p>
        ) : (
          <ul className="divide-y divide-slate-100">
            {appointments.map((appointment) => (
              <li key={appointment.id} className="flex items-center justify-between gap-4 px-6 py-4">
                <div>
                  <p className="font-medium">{formatDateTime(appointment.startUtc, settings.timeZoneId)}</p>
                  <p className="text-sm text-slate-600">
                    {appointment.customerName} · {appointment.customerEmail}
                    {appointment.customerPhone ? ` · ${appointment.customerPhone}` : ''}
                  </p>
                  {appointment.notes && <p className="mt-1 text-sm text-slate-400">“{appointment.notes}”</p>}
                </div>
                <div className="flex items-center gap-3">
                  <span className={`rounded-full px-3 py-1 text-xs font-medium ${statusStyles[appointment.status]}`}>
                    {appointment.status}
                  </span>
                  {appointment.status === 'Scheduled' && (
                    <button
                      onClick={() => cancel(appointment.id)}
                      className="text-sm text-rose-600 transition hover:text-rose-800"
                    >
                      Cancel
                    </button>
                  )}
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  )
}
