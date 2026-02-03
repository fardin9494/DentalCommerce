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

export function toJalaliDateString(value?: string | Date | null) {
  if (!value) return ''
  const dt = value instanceof Date ? value : new Date(value)
  if (Number.isNaN(dt.getTime())) return ''
  const { jy, jm, jd } = gregorianToJalali(dt.getFullYear(), dt.getMonth() + 1, dt.getDate())
  return `${jy}/${pad2(jm)}/${pad2(jd)}`
}

export function jalaliInputToIso(value?: string | null) {
  if (!value) return null
  const parsed = parseJalaliDate(value)
  if (!parsed) return null
  const { gy, gm, gd } = jalaliToGregorian(parsed.jy, parsed.jm, parsed.jd)
  const date = new Date(Date.UTC(gy, gm - 1, gd, 0, 0, 0))
  return date.toISOString()
}

function parseJalaliDate(value: string) {
  const normalized = value.trim().replace(/-/g, '/')
  const parts = normalized.split('/')
  if (parts.length !== 3) return null
  const jy = Number(parts[0])
  const jm = Number(parts[1])
  const jd = Number(parts[2])
  if (!Number.isFinite(jy) || !Number.isFinite(jm) || !Number.isFinite(jd)) return null
  if (jm < 1 || jm > 12) return null
  if (jd < 1 || jd > 31) return null
  return { jy, jm, jd }
}

function gregorianToJalali(gy: number, gm: number, gd: number) {
  const g_d_m = [0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334]
  let jy = 0
  let gy2 = gy - 1600
  let gm2 = gm - 1
  let gd2 = gd - 1
  let g_day_no = 365 * gy2 + div(gy2 + 3, 4) - div(gy2 + 99, 100) + div(gy2 + 399, 400)
  g_day_no += g_d_m[gm2] + gd2
  if (gm2 > 1 && isGregorianLeap(gy)) g_day_no++
  let j_day_no = g_day_no - 79
  const j_np = div(j_day_no, 12053)
  j_day_no %= 12053
  jy = 979 + 33 * j_np + 4 * div(j_day_no, 1461)
  j_day_no %= 1461
  if (j_day_no >= 366) {
    jy += div(j_day_no - 1, 365)
    j_day_no = (j_day_no - 1) % 365
  }
  let jm = j_day_no < 186 ? 1 + div(j_day_no, 31) : 7 + div(j_day_no - 186, 30)
  let jd = 1 + (j_day_no < 186 ? j_day_no % 31 : (j_day_no - 186) % 30)
  return { jy, jm, jd }
}

function jalaliToGregorian(jy: number, jm: number, jd: number) {
  jy += 1595
  let days = -355668 + 365 * jy + div(jy, 33) * 8 + div((jy % 33) + 3, 4) + jd
  days += jm < 7 ? (jm - 1) * 31 : (jm - 7) * 30 + 186
  let gy = 400 * div(days, 146097)
  days %= 146097
  if (days > 36524) {
    gy += 100 * div(--days, 36524)
    days %= 36524
    if (days >= 365) days++
  }
  gy += 4 * div(days, 1461)
  days %= 1461
  if (days > 365) {
    gy += div(days - 1, 365)
    days = (days - 1) % 365
  }
  let gd = days + 1
  const sal = [0, 31, isGregorianLeap(gy) ? 29 : 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31]
  let gm = 0
  for (gm = 1; gm <= 12 && gd > sal[gm]; gm++) gd -= sal[gm]
  return { gy, gm, gd }
}

function isGregorianLeap(gy: number) {
  return (gy % 4 === 0 && gy % 100 !== 0) || gy % 400 === 0
}

function div(a: number, b: number) {
  return Math.floor(a / b)
}

function pad2(value: number) {
  return value < 10 ? `0${value}` : String(value)
}
