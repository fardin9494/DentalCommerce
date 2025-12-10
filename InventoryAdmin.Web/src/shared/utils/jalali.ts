// Minimal Jalali (Persian) calendar converter (adapted from jalaali-js)
// Converts Jalali date (jy, jm, jd) to Gregorian JS Date

type JalaliTuple = [number, number, number]

function div(a: number, b: number) {
  return Math.floor(a / b)
}

// Returns jd for a given Gregorian date
function g2d(gy: number, gm: number, gd: number) {
  const d =
    div(gy + div(gm - 8, 6) + 100100, 400) -
    div(gy + div(gm - 8, 6) + 100100, 100) +
    div(gm + 9, 12) +
    gd +
    365 * gy -
    34840408
  return d - 1
}

// Returns Jalali year for Julian day number
function d2j(jdn: number): JalaliTuple {
  const gy = d2g(jdn)[0]
  let jy = gy - 621
  const r = jalCal(jy)
  const jdn1f = g2d(gy, 3, r.march)
  let k = jdn - jdn1f
  let jm: number
  let jd: number
  if (k >= 0) {
    if (k <= 185) {
      jm = 1 + div(k, 31)
      jd = (k % 31) + 1
      return [jy, jm, jd]
    } else {
      k -= 186
    }
  } else {
    jy -= 1
    k += 179
    if (r.leap === 1) k += 1
  }
  jm = 7 + div(k, 30)
  jd = (k % 30) + 1
  return [jy, jm, jd]
}

function jalCal(jy: number) {
  const breaks = [
    -61, 9, 38, 199, 426, 686, 756, 818, 1111, 1181, 1210,
    1635, 2060, 2097, 2192, 2262, 2324, 2394, 2456, 3178,
  ]
  let bl = breaks.length
  let gy = jy + 621
  let leapJ = -14
  let jp = breaks[0]
  let jump = 0
  for (let i = 1; i < bl; i += 1) {
    const jm = breaks[i]
    jump = jm - jp
    if (jy < jm) break
    leapJ = leapJ + div(jump, 33) * 8 + div((jump % 33) + 3, 4)
    jp = jm
  }
  const n = jy - jp
  leapJ = leapJ + div(n, 33) * 8 + div((n % 33) + 3, 4)
  if ((jump % 33) === 4 && jump - n === 4) leapJ += 1
  const leapG = div(gy, 4) - div((div(gy, 100) + 1) * 3, 4) - 150
  const march = 20 + leapJ - leapG
  return { leap: (((n + 1) % 33) - 1) % 4, gy, march }
}

function d2g(jdn: number): JalaliTuple {
  let j = 4 * jdn + 139361631
  j = j + 4 * div(3 * div(j, 146097), 4) - 3908
  const i = div(j % 1461, 4) * 5 + 308
  const gd = div((i % 153), 5) + 1
  const gm = ((div(i, 153) % 12) + 1)
  const gy = div(j, 1461) - 100100 + div(8 - gm, 6)
  return [gy, gm, gd]
}

// Convert Jalali date to Gregorian Date object
export function jalaliToGregorianDate(jy: number, jm: number, jd: number): Date {
  const r = jalCal(jy)
  const gy = r.gy
  const march = r.march
  let gDayNo = g2d(gy, 3, march)
  let jDayNo = (jm <= 6 ? (jm - 1) * 31 : (jm - 7) * 30 + 186) + jd - 1
  gDayNo += jDayNo
  const [gY, gM, gD] = d2g(gDayNo)
  return new Date(Date.UTC(gY, gM - 1, gD))
}

// Parse input like "1402-01-15" or "1402/01/15"
export function parseJalaliInput(input: string): { jy: number; jm: number; jd: number } | null {
  if (!input) return null
  const clean = input.trim().replace(/-/g, '/')
  const parts = clean.split('/')
  if (parts.length !== 3) return null
  const [jyStr, jmStr, jdStr] = parts
  const jy = Number(jyStr)
  const jm = Number(jmStr)
  const jd = Number(jdStr)
  if (
    !Number.isInteger(jy) ||
    !Number.isInteger(jm) ||
    !Number.isInteger(jd) ||
    jm < 1 || jm > 12 ||
    jd < 1 || jd > 31
  ) {
    return null
  }
  return { jy, jm, jd }
}

