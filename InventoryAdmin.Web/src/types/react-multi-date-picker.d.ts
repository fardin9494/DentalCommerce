declare module 'react-multi-date-picker' {
  import { ComponentType } from 'react'
  import { DateObject } from 'react-date-object'

  export interface DatePickerProps {
    value?: DateObject | string | number | null | (DateObject | string | number | null)[]
    onChange?: (value: DateObject | DateObject[] | null) => void
    calendar?: any
    locale?: any
    calendarPosition?: string
    editable?: boolean
    inputClass?: string
    placeholder?: string
    format?: string
    minDate?: DateObject | string | number
    maxDate?: DateObject | string | number
  }

  const DatePicker: ComponentType<DatePickerProps>
  export default DatePicker
}

declare module 'react-date-object' {
  export default class DateObject {
    constructor(params?: any)
    year: number
    month: { number: number }
    day: number
    toDate(): Date
  }
}

declare module 'react-date-object/calendars/persian' {
  const calendar: any
  export default calendar
}

declare module 'react-date-object/locales/persian_fa' {
  const locale: any
  export default locale
}

