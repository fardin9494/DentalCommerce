import { useEffect, useMemo, useState, type ChangeEvent } from 'react'
import { Spinner } from '@/shared/components/Spinner'
import { useReceiptLineSerials, useSetReceiptLineSerials } from '../queries'
import type { ReceiptLine } from '../types'

interface ReceiptLineSerialsModalProps {
  isOpen: boolean
  receiptId: string
  line: ReceiptLine | null
  productName?: string
  variantName?: string
  onClose: () => void
}

function parseSerials(text: string) {
  return text
    .split(/[\r\n\t,;]+/g)
    .map((item) => item.trim())
    .filter(Boolean)
}

function mergeSerials(current: string[], incoming: string[]) {
  const map = new Map<string, string>()
  current.forEach((serial) => {
    map.set(serial.toLowerCase(), serial)
  })
  incoming.forEach((serial) => {
    const key = serial.toLowerCase()
    if (!map.has(key)) {
      map.set(key, serial)
    }
  })
  return Array.from(map.values())
}

export function ReceiptLineSerialsModal({
  isOpen,
  receiptId,
  line,
  productName,
  variantName,
  onClose,
}: ReceiptLineSerialsModalProps) {
  const [inputText, setInputText] = useState('')
  const [serials, setSerials] = useState<string[]>([])
  const { data, isLoading } = useReceiptLineSerials(receiptId, line?.id, isOpen)
  const saveSerials = useSetReceiptLineSerials(receiptId, line?.id)

  const expectedCount = useMemo(() => {
    if (!line) return null
    return Number.isInteger(line.qty) ? Math.trunc(line.qty) : null
  }, [line])

  const hasMismatch = useMemo(() => {
    if (serials.length === 0) return false
    if (expectedCount === null) return true
    return serials.length !== expectedCount
  }, [expectedCount, serials.length])

  useEffect(() => {
    if (!isOpen) return
    if (data) {
      setSerials(data.map((item) => item.serialNumber))
    } else {
      setSerials([])
    }
    setInputText('')
  }, [isOpen, data])

  if (!isOpen || !line) return null

  function handleAddFromText() {
    if (!inputText.trim()) return
    const parsed = parseSerials(inputText)
    setSerials((prev) => mergeSerials(prev, parsed))
    setInputText('')
  }

  function handleFileChange(e: ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    if (!file) return
    const reader = new FileReader()
    reader.onload = () => {
      const content = String(reader.result || '')
      const parsed = parseSerials(content)
      setSerials((prev) => mergeSerials(prev, parsed))
    }
    reader.readAsText(file)
    e.target.value = ''
  }

  function handleRemove(serial: string) {
    setSerials((prev) => prev.filter((item) => item !== serial))
  }

  async function handleSave() {
    await saveSerials.mutateAsync(serials)
    onClose()
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
      <div className="w-full max-w-3xl rounded-xl bg-white shadow-xl">
        <div className="border-b border-slate-200 px-6 py-4">
          <h3 className="text-lg font-semibold text-slate-900">مدیریت سریال‌ها</h3>
          <p className="mt-1 text-sm text-slate-500">
            {productName || '-'}{variantName ? ` / ${variantName}` : ''} - خط {line.lineNo}
          </p>
        </div>

        <div className="px-6 py-4 space-y-4">
          <div className="rounded-lg border border-slate-200 bg-slate-50 p-3 text-sm">
            <div className="flex items-center justify-between">
              <span className="text-slate-600">مقدار خط</span>
              <span className="font-medium text-slate-900">{line.qty.toLocaleString('fa-IR')}</span>
            </div>
            <div className="mt-2 flex items-center justify-between">
              <span className="text-slate-600">تعداد سریال‌ها</span>
              <span className={`font-medium ${hasMismatch ? 'text-amber-700' : 'text-slate-900'}`}>
                {serials.length.toLocaleString('fa-IR')}
              </span>
            </div>
            {expectedCount !== null && (
              <div className="mt-2 text-xs text-slate-500">
                انتظار: {expectedCount.toLocaleString('fa-IR')} سریال
              </div>
            )}
            {hasMismatch && expectedCount !== null && (
              <div className="mt-2 text-xs text-amber-700">
                تعداد سریال‌ها با مقدار خط یکسان نیست.
              </div>
            )}
            {hasMismatch && expectedCount === null && (
              <div className="mt-2 text-xs text-amber-700">
                برای ثبت سریال، مقدار خط باید عدد صحیح باشد.
              </div>
            )}
          </div>

          <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
            <div className="space-y-2">
              <label className="text-sm font-medium text-slate-700">ورود دستی یا چسباندن لیست</label>
              <textarea
                value={inputText}
                onChange={(e) => setInputText(e.target.value)}
                rows={6}
                className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20"
                placeholder="هر سریال را در یک خط جدا وارد کنید"
              />
              <div className="flex items-center gap-2">
                <button
                  type="button"
                  onClick={handleAddFromText}
                  className="rounded-lg bg-emerald-600 px-3 py-1.5 text-xs font-medium text-white transition-colors hover:bg-emerald-700"
                >
                  افزودن
                </button>
                <label className="inline-flex cursor-pointer items-center gap-2 rounded-lg border border-slate-300 bg-white px-3 py-1.5 text-xs font-medium text-slate-700 transition-colors hover:bg-slate-50">
                  <input
                    type="file"
                    accept=".csv,.txt,.tsv"
                    onChange={handleFileChange}
                    className="hidden"
                  />
                  بارگذاری فایل (CSV/TXT)
                </label>
              </div>
              <p className="text-[11px] text-slate-500">
                فایل اکسل را به CSV خروجی بگیرید و سپس بارگذاری کنید.
              </p>
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium text-slate-700">لیست سریال‌ها</label>
              <div className="h-48 overflow-y-auto rounded-lg border border-slate-200 bg-white p-2">
                {isLoading ? (
                  <div className="flex items-center justify-center py-10 text-slate-500">
                    <Spinner className="h-4 w-4 text-emerald-600" />
                  </div>
                ) : serials.length === 0 ? (
                  <div className="py-10 text-center text-sm text-slate-400">
                    سریالی ثبت نشده است
                  </div>
                ) : (
                  <div className="flex flex-col gap-1">
                    {serials.map((serial) => (
                      <div
                        key={serial}
                        className="flex items-center justify-between rounded-md border border-slate-100 px-2 py-1 text-xs text-slate-700"
                      >
                        <span className="font-mono">{serial}</span>
                        <button
                          type="button"
                          onClick={() => handleRemove(serial)}
                          className="rounded px-2 py-0.5 text-xs text-red-600 hover:bg-red-50"
                        >
                          حذف
                        </button>
                      </div>
                    ))}
                  </div>
                )}
              </div>
              {serials.length > 0 && (
                <button
                  type="button"
                  onClick={() => setSerials([])}
                  className="text-xs text-slate-500 hover:text-red-600"
                >
                  پاک کردن همه
                </button>
              )}
            </div>
          </div>
        </div>

        <div className="flex items-center justify-end gap-3 border-t border-slate-200 px-6 py-4">
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-50"
          >
            انصراف
          </button>
          <button
            type="button"
            onClick={handleSave}
            disabled={saveSerials.isPending || hasMismatch}
            className="rounded-lg bg-emerald-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-emerald-700 disabled:opacity-50"
          >
            {saveSerials.isPending ? 'در حال ذخیره...' : 'ذخیره سریال‌ها'}
          </button>
        </div>
      </div>
    </div>
  )
}
