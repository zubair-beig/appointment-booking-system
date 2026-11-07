import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { ApiError, bookingApi, type BookingConfirmation, type BookingSettings, type Slot } from '../api'
import { addDays, formatDayChip, formatLongDate, formatTime, todayIn } from '../dates'

interface BookingPageProps {
  settings: BookingSettings
}

export default function BookingPage({ settings }: BookingPageProps) {
  const today = useMemo(() => todayIn(settings.timeZoneId), [settings.timeZoneId])
  const days = useMemo(
    () => Array.from({ length: settings.bookingWindowDays + 1 }, (_, offset) => addDays(today, offset)),
    [today, settings.bookingWindowDays],
  )

  const [selectedDate, setSelectedDate] = useState(today)
  const [slots, setSlots] = useState<Slot[] | null>(null)
  const [selectedSlot, setSelectedSlot] = useState<Slot | null>(null)
  const [confirmation, setConfirmation] = useState<BookingConfirmation | null>(null)

  const loadSlots = (date: string) => {
    setSlots(null)
    bookingApi.getSlots(date).then(setSlots).catch(() => setSlots([]))
  }

  useEffect(() => {
    loadSlots(selectedDate)
  }, [selectedDate])

  if (confirmation) {
    return (
      <div className="mx-auto max-w-md rounded-3xl bg-white p-10 text-center shadow-xl shadow-indigo-100">
        <div className="mx-auto mb-4 grid h-14 w-14 place-items-center rounded-full bg-emerald-100 text-2xl text-emerald-600">
          ✓
        </div>
        <h2 className="text-2xl font-semibold">You're booked!</h2>
        <p className="mt-2 text-slate-500">
          {formatLongDate(selectedDate)} at {formatTime(confirmation.startUtc, settings.timeZoneId)}
        </p>
        <button
          onClick={() => {
            setConfirmation(null)
            setSelectedSlot(null)
            loadSlots(selectedDate)
          }}
          className="mt-8 rounded-xl bg-indigo-600 px-5 py-2.5 text-sm font-medium text-white transition hover:bg-indigo-700"
        >
          Book another
        </button>
      </div>
    )
  }

  const availableSlots = slots?.filter((slot) => slot.isAvailable) ?? []

  return (
    <div className="grid gap-8 md:grid-cols-[1fr_20rem]">
      <section>
        <h1 className="text-3xl font-semibold tracking-tight">Pick a time</h1>
        <p className="mt-1 text-slate-500">
          {settings.slotMinutes}-minute appointments · times shown in {settings.timeZoneId}
        </p>

        <div className="-mx-1 mt-6 flex gap-2 overflow-x-auto px-1 pb-3">
          {days.map((date) => {
            const chip = formatDayChip(date)
            const isSelected = date === selectedDate
            return (
              <button
                key={date}
                onClick={() => {
                  setSelectedDate(date)
                  setSelectedSlot(null)
                }}
                className={`flex w-16 shrink-0 flex-col items-center rounded-2xl border px-2 py-3 transition ${
                  isSelected
                    ? 'border-indigo-600 bg-indigo-600 text-white shadow-lg shadow-indigo-200'
                    : 'border-slate-200 bg-white hover:border-indigo-300'
                }`}
              >
                <span className="text-xs uppercase opacity-70">{chip.weekday}</span>
                <span className="text-xl font-semibold">{chip.day}</span>
                <span className="text-xs opacity-70">{chip.month}</span>
              </button>
            )
          })}
        </div>

        <h2 className="mt-4 text-sm font-medium text-slate-500">{formatLongDate(selectedDate)}</h2>
        {slots === null ? (
          <p className="mt-4 text-slate-400">Loading times…</p>
        ) : availableSlots.length === 0 ? (
          <p className="mt-4 rounded-2xl bg-white p-6 text-slate-500 ring-1 ring-slate-200">
            No free times on this day. Try another date.
          </p>
        ) : (
          <div className="mt-3 grid grid-cols-3 gap-3 sm:grid-cols-4">
            {availableSlots.map((slot) => {
              const isSelected = selectedSlot?.startUtc === slot.startUtc
              return (
                <button
                  key={slot.startUtc}
                  onClick={() => setSelectedSlot(slot)}
                  className={`rounded-xl border py-3 text-sm font-medium transition ${
                    isSelected
                      ? 'border-indigo-600 bg-indigo-50 text-indigo-700 ring-2 ring-indigo-200'
                      : 'border-slate-200 bg-white hover:border-indigo-400 hover:text-indigo-600'
                  }`}
                >
                  {formatTime(slot.startUtc, settings.timeZoneId)}
                </button>
              )
            })}
          </div>
        )}
      </section>

      <aside>
        <DetailsForm
          slot={selectedSlot}
          timeZoneId={settings.timeZoneId}
          onBooked={setConfirmation}
          onSlotTaken={() => {
            setSelectedSlot(null)
            loadSlots(selectedDate)
          }}
        />
      </aside>
    </div>
  )
}

interface DetailsFormProps {
  slot: Slot | null
  timeZoneId: string
  onBooked: (confirmation: BookingConfirmation) => void
  onSlotTaken: () => void
}

function DetailsForm({ slot, timeZoneId, onBooked, onSlotTaken }: DetailsFormProps) {
  const [customerName, setCustomerName] = useState('')
  const [customerEmail, setCustomerEmail] = useState('')
  const [customerPhone, setCustomerPhone] = useState('')
  const [notes, setNotes] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    if (!slot) return
    setIsSubmitting(true)
    setErrorMessage(null)
    try {
      const confirmation = await bookingApi.book({
        startUtc: slot.startUtc,
        customerName,
        customerEmail,
        customerPhone: customerPhone || undefined,
        notes: notes || undefined,
      })
      onBooked(confirmation)
    } catch (error) {
      setErrorMessage(error instanceof Error ? error.message : 'Something went wrong.')
      if (error instanceof ApiError && error.status === 409) onSlotTaken()
    } finally {
      setIsSubmitting(false)
    }
  }

  const inputClass =
    'w-full rounded-xl border border-slate-200 bg-white px-3 py-2.5 text-sm outline-none transition focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100'

  return (
    <form onSubmit={submit} className="sticky top-6 space-y-4 rounded-3xl bg-white p-6 shadow-xl shadow-indigo-100/60">
      <h2 className="text-lg font-semibold">Your details</h2>
      <p className="rounded-xl bg-slate-50 px-3 py-2 text-sm text-slate-600">
        {slot ? `Selected: ${formatTime(slot.startUtc, timeZoneId)}` : 'Choose a time to continue'}
      </p>
      <input required placeholder="Full name" value={customerName} onChange={(e) => setCustomerName(e.target.value)} className={inputClass} />
      <input required type="email" placeholder="Email" value={customerEmail} onChange={(e) => setCustomerEmail(e.target.value)} className={inputClass} />
      <input placeholder="Phone (optional)" value={customerPhone} onChange={(e) => setCustomerPhone(e.target.value)} className={inputClass} />
      <textarea placeholder="Anything we should know? (optional)" rows={3} value={notes} onChange={(e) => setNotes(e.target.value)} className={inputClass} />
      {errorMessage && <p className="rounded-xl bg-rose-50 px-3 py-2 text-sm text-rose-700">{errorMessage}</p>}
      <button
        disabled={!slot || isSubmitting}
        className="w-full rounded-xl bg-indigo-600 py-3 text-sm font-semibold text-white shadow-lg shadow-indigo-200 transition hover:bg-indigo-700 disabled:cursor-not-allowed disabled:bg-slate-300 disabled:shadow-none"
      >
        {isSubmitting ? 'Booking…' : 'Confirm booking'}
      </button>
    </form>
  )
}
