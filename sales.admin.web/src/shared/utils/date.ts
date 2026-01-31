export function formatDate(value?: string | Date | null) {
  if (!value) return '—'
  const dt = value instanceof Date ? value : new Date(value)
  if (Number.isNaN(dt.getTime())) return String(value)
  return dt.toLocaleString('fa-IR')
}

export function formatNumber(value?: number | null) {
  if (value === null || value === undefined) return '—'
  return new Intl.NumberFormat('fa-IR').format(value)
}
