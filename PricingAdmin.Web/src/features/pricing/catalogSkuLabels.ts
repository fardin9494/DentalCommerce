const STORAGE_KEY = 'pricingAdmin.skuLabels.v1'

type SkuLabelMap = Record<string, string>

function normalizeSkuKey(skuId: string) {
  return skuId.trim().toLowerCase()
}

function readMap(): SkuLabelMap {
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY)
    if (!raw) return {}
    const parsed = JSON.parse(raw) as unknown
    if (!parsed || typeof parsed !== 'object') return {}
    return parsed as SkuLabelMap
  } catch {
    return {}
  }
}

function writeMap(map: SkuLabelMap) {
  try {
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(map))
  } catch {
    // ignore
  }
}

export function getSkuLabel(skuId: string): string | null {
  const key = normalizeSkuKey(skuId)
  if (!key) return null
  const map = readMap()
  const value = map[key]
  return value ? String(value) : null
}

export function setSkuLabel(skuId: string, label: string) {
  const key = normalizeSkuKey(skuId)
  const cleanLabel = label.trim()
  if (!key || !cleanLabel) return

  const map = readMap()
  map[key] = cleanLabel
  writeMap(map)
}

export function formatSkuLabel(skuId: string) {
  const label = getSkuLabel(skuId)
  if (!label) return null
  if (label.trim().toLowerCase() === skuId.trim().toLowerCase()) return null
  return label
}

