import { useEffect, useState } from 'react'
import { bookingApi, type BookingSettings } from './api'
import BookingPage from './pages/BookingPage'
import AdminPage from './pages/AdminPage'

/** Minimal hash router: "#/" is the public booking page, "#/admin" is the owner view. */
function useHashRoute(): string {
  const [route, setRoute] = useState(window.location.hash || '#/')
  useEffect(() => {
    const onChange = () => setRoute(window.location.hash || '#/')
    window.addEventListener('hashchange', onChange)
    return () => window.removeEventListener('hashchange', onChange)
  }, [])
  return route
}

export default function App() {
  const route = useHashRoute()
  const [settings, setSettings] = useState<BookingSettings | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)

  useEffect(() => {
    bookingApi.getSettings().then(setSettings).catch((error: Error) => setLoadError(error.message))
  }, [])

  return (
    <div className="min-h-screen bg-gradient-to-b from-indigo-50 via-slate-50 to-slate-50">
      <header className="mx-auto flex max-w-4xl items-center justify-between px-6 py-6">
        <a href="#/" className="flex items-center gap-2 text-lg font-semibold tracking-tight">
          <span className="grid h-8 w-8 place-items-center rounded-xl bg-indigo-600 text-white">◷</span>
          {settings?.businessName ?? 'Appointment Book'}
        </a>
        <a
          href={route === '#/admin' ? '#/' : '#/admin'}
          className="text-sm text-slate-500 transition hover:text-indigo-600"
        >
          {route === '#/admin' ? 'Back to booking' : 'Owner login'}
        </a>
      </header>

      <main className="mx-auto max-w-4xl px-6 pb-16">
        {loadError && (
          <p className="rounded-xl bg-rose-50 p-4 text-sm text-rose-700">
            Could not reach the booking service: {loadError}
          </p>
        )}
        {settings && (route === '#/admin' ? <AdminPage settings={settings} /> : <BookingPage settings={settings} />)}
      </main>
    </div>
  )
}
