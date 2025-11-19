export function formatJalaliDate(input: string | number | Date | null | undefined): string {
  if (input == null) return ''

  const date = input instanceof Date ? input : new Date(input)
  if (Number.isNaN(date.getTime())) return ''

  try {
    // استفاده از تقویم فارسی (شمسی) در Intl
    return new Intl.DateTimeFormat('fa-IR-u-ca-persian', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
    }).format(date)
  } catch {
    // در صورت عدم پشتیبانی، حداقل به‌صورت تاریخ فارسی عادی نمایش بده
    return date.toLocaleDateString('fa-IR')
  }
}

