import { useEffect, useMemo, useState } from 'react'
import DatePicker from 'react-multi-date-picker'
import DateObject from 'react-date-object'
import persian from 'react-date-object/calendars/persian'
import persian_fa from 'react-date-object/locales/persian_fa'

type Props = {
  value: string
  onChange: (next: string) => void
  placeholder?: string
  clearable?: boolean
  showTime?: boolean
}

export function JalaliDateTimePicker(props: Props) {
  const showTime = props.showTime ?? true

  const parsed = useMemo(() => parseDateTimeLocalOrIso(props.value), [props.value])

  const [dateObj, setDateObj] = useState<DateObject | null>(null)
  const [hour, setHour] = useState<number>(0)
  const [minute, setMinute] = useState<number>(0)

  useEffect(() => {
    if (!parsed) {
      setDateObj(null)
      setHour(0)
      setMinute(0)
      return
    }

    setDateObj(new DateObject({ date: parsed, calendar: persian, locale: persian_fa }))
    setHour(parsed.getHours())
    setMinute(parsed.getMinutes())
  }, [parsed])

  const hours = useMemo(() => Array.from({ length: 24 }, (_, i) => i), [])
  const minutes = useMemo(() => Array.from({ length: 12 }, (_, i) => i * 5), [])

  const commit = (nextDate: Date, nextHour: number, nextMinute: number) => {
    const next = formatLocalDateTime(nextDate, nextHour, nextMinute, showTime)
    props.onChange(next)
  }

  const displayDate = dateObj ? dateObj.format('YYYY/MM/DD') : ''
  const displayTime = showTime ? `${pad2(hour)}:${pad2(minute)}` : ''
  const displayValue = displayDate ? (showTime ? `${displayDate} ${displayTime}` : displayDate) : ''

  return (
    <div className="flex flex-col sm:flex-row gap-2 items-end">
      <div className="flex-1 min-w-0">
        <DatePicker
          value={dateObj ?? undefined}
          onChange={(val) => {
            const d = Array.isArray(val) ? (val[0] as DateObject | null) : (val as DateObject | null)
            setDateObj(d)
            if (!d) {
              props.onChange('')
              return
            }
            commit(d.toDate(), hour, minute)
          }}
          calendar={persian}
          locale={persian_fa}
          calendarPosition="bottom-center"
          editable={false}
          portal
          render={(value, openCalendar) => {
            return (
              <input
                className="input text-right w-full cursor-pointer"
                value={displayValue}
                placeholder={props.placeholder || 'انتخاب تاریخ (شمسی)'}
                readOnly
                onClick={openCalendar}
              />
            )
          }}
        />
      </div>

      {showTime && (
        <div className="flex items-end gap-1.5 flex-shrink-0">
          <div className="w-14">
            <label className="block text-xs text-gray-600 mb-1">ساعت</label>
            <select
              className="input text-center py-2 text-xs px-0.5"
              value={hour}
              disabled={!dateObj}
              onChange={(e) => {
                const h = Number(e.target.value)
                setHour(h)
                if (!dateObj) return
                commit(dateObj.toDate(), h, minute)
              }}
            >
              {hours.map(h => (
                <option key={h} value={h}>{pad2(h)}</option>
              ))}
            </select>
          </div>
          <div className="w-14">
            <label className="block text-xs text-gray-600 mb-1">دقیقه</label>
            <select
              className="input text-center py-2 text-xs px-0.5"
              value={minute}
              disabled={!dateObj}
              onChange={(e) => {
                const m = Number(e.target.value)
                setMinute(m)
                if (!dateObj) return
                commit(dateObj.toDate(), hour, m)
              }}
            >
              {minutes.map(m => (
                <option key={m} value={m}>{pad2(m)}</option>
              ))}
            </select>
          </div>
          {props.clearable && (
            <button
              type="button"
              className="btn-secondary px-2.5 py-2 rounded text-xs whitespace-nowrap h-[42px]"
              onClick={() => props.onChange('')}
              disabled={!props.value}
              title="پاک کردن تاریخ"
            >
              پاک
            </button>
          )}
        </div>
      )}
    </div>
  )
}

function parseDateTimeLocalOrIso(value: string) {
  const trimmed = (value || '').trim()
  if (!trimmed) return null
  const d = new Date(trimmed)
  if (!Number.isFinite(d.getTime())) return null
  return d
}

function formatLocalDateTime(date: Date, hour: number, minute: number, includeTime: boolean) {
  const y = date.getFullYear()
  const m = date.getMonth() + 1
  const d = date.getDate()
  const datePart = `${y}-${pad2(m)}-${pad2(d)}`
  if (!includeTime) return `${datePart}T00:00`
  return `${datePart}T${pad2(hour)}:${pad2(minute)}`
}

function pad2(n: number) {
  return String(n).padStart(2, '0')
}
