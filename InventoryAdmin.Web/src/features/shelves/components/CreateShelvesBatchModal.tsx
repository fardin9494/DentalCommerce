import { useState } from 'react'
import { Spinner } from '@/shared/components/Spinner'
import { useActiveWarehouses } from '@/shared/hooks/useWarehouses'

interface CreateShelvesBatchModalProps {
  isOpen: boolean
  onClose: () => void
  onSubmit: (data: {
    warehouseId: string
    rows: number
    columns: number
    levels?: number
    prefix?: string
    description?: string
  }) => Promise<void>
  isSubmitting?: boolean
}

export function CreateShelvesBatchModal({
  isOpen,
  onClose,
  onSubmit,
  isSubmitting = false,
}: CreateShelvesBatchModalProps) {
  const { data: warehouses, isLoading: loadingWarehouses } = useActiveWarehouses()
  const [warehouseId, setWarehouseId] = useState('')
  const [rows, setRows] = useState('1')
  const [columns, setColumns] = useState('1')
  const [levels, setLevels] = useState('1')
  const [prefix, setPrefix] = useState('')
  const [description, setDescription] = useState('')
  const [errors, setErrors] = useState<Record<string, string>>({})

  if (!isOpen) return null

  function validate() {
    const errs: Record<string, string> = {}
    if (!warehouseId) errs.warehouseId = 'انبار الزامی است'
    const r = Number(rows)
    const c = Number(columns)
    const l = Number(levels)
    if (!r || r <= 0) errs.rows = 'تعداد ردیف نامعتبر است'
    if (!c || c <= 0) errs.columns = 'تعداد ستون نامعتبر است'
    if (!l || l <= 0) errs.levels = 'تعداد طبقه نامعتبر است'
    setErrors(errs)
    return Object.keys(errs).length === 0
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!validate()) return
    await onSubmit({
      warehouseId,
      rows: Number(rows),
      columns: Number(columns),
      levels: Number(levels),
      prefix: prefix || undefined,
      description: description || undefined,
    })
    handleClose()
  }

  function handleClose() {
    if (isSubmitting) return
    setErrors({})
    setRows('1')
    setColumns('1')
    setLevels('1')
    setPrefix('')
    setDescription('')
    onClose()
  }

  const total = (Number(rows) || 0) * (Number(columns) || 0) * (Number(levels) || 0)

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={handleClose} />

      <div className="relative w-full max-w-3xl rounded-2xl bg-white p-6 shadow-2xl">
        <div className="mb-6 flex items-center justify-between">
          <div>
            <h2 className="text-xl font-bold text-slate-900">ایجاد گروهی قفسه‌ها</h2>
            <p className="text-sm text-slate-600 mt-1">
              انبار را انتخاب کنید و تعداد ردیف، ستون و طبقات را وارد کنید. کدها به صورت خودکار ساخته می‌شوند.
            </p>
          </div>
          <button
            onClick={handleClose}
            disabled={isSubmitting}
            className="rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-600 disabled:opacity-50"
          >
            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" d="M6 18 18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-5">
          <div className="grid gap-4 md:grid-cols-2">
            <div>
              <label className="mb-1 block text-sm font-medium text-slate-700">انبار</label>
              {loadingWarehouses ? (
                <div className="flex items-center gap-2 text-sm text-slate-500">
                  <Spinner className="h-4 w-4 text-emerald-600" />
                  در حال بارگذاری انبارها...
                </div>
              ) : (
                <select
                  value={warehouseId}
                  onChange={(e) => setWarehouseId(e.target.value)}
                  className={`w-full rounded-lg border px-3 py-2 text-sm focus:outline-none focus:ring-2 ${
                    errors.warehouseId
                      ? 'border-red-300 focus:border-red-500 focus:ring-red-500/20'
                      : 'border-slate-300 focus:border-emerald-500 focus:ring-emerald-500/20'
                  }`}
                  disabled={isSubmitting}
                >
                  <option value="">انتخاب کنید...</option>
                  {warehouses?.map((w) => (
                    <option key={w.id} value={w.id}>
                      {w.name} ({w.code})
                    </option>
                  ))}
                </select>
              )}
              {errors.warehouseId && <p className="mt-1 text-xs text-red-600">{errors.warehouseId}</p>}
            </div>

            <div>
              <label className="mb-1 block text-sm font-medium text-slate-700">پیشوند کد (اختیاری)</label>
              <input
                type="text"
                value={prefix}
                onChange={(e) => setPrefix(e.target.value)}
                placeholder="مثال: WH01"
                className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500"
                disabled={isSubmitting}
              />
              <p className="mt-1 text-xs text-slate-500">
                اگر خالی بماند از کد انبار استفاده می‌شود. کد نهایی: PREFIX-ROWCOL-L
              </p>
            </div>
          </div>

          <div className="grid gap-4 md:grid-cols-3">
            <div>
              <label className="mb-1 block text-sm font-medium text-slate-700">تعداد ردیف</label>
              <input
                type="number"
                min="1"
                value={rows}
                onChange={(e) => setRows(e.target.value)}
                className={`w-full rounded-lg border px-3 py-2 text-sm focus:outline-none focus:ring-2 ${
                  errors.rows
                    ? 'border-red-300 focus:border-red-500 focus:ring-red-500/20'
                    : 'border-slate-300 focus:border-emerald-500 focus:ring-emerald-500/20'
                }`}
                disabled={isSubmitting}
              />
              {errors.rows && <p className="mt-1 text-xs text-red-600">{errors.rows}</p>}
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium text-slate-700">تعداد ستون/قفسه در هر ردیف</label>
              <input
                type="number"
                min="1"
                value={columns}
                onChange={(e) => setColumns(e.target.value)}
                className={`w-full rounded-lg border px-3 py-2 text-sm focus:outline-none focus:ring-2 ${
                  errors.columns
                    ? 'border-red-300 focus:border-red-500 focus:ring-red-500/20'
                    : 'border-slate-300 focus:border-emerald-500 focus:ring-emerald-500/20'
                }`}
                disabled={isSubmitting}
              />
              {errors.columns && <p className="mt-1 text-xs text-red-600">{errors.columns}</p>}
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium text-slate-700">تعداد طبقه</label>
              <input
                type="number"
                min="1"
                value={levels}
                onChange={(e) => setLevels(e.target.value)}
                className={`w-full rounded-lg border px-3 py-2 text-sm focus:outline-none focus:ring-2 ${
                  errors.levels
                    ? 'border-red-300 focus:border-red-500 focus:ring-red-500/20'
                    : 'border-slate-300 focus:border-emerald-500 focus:ring-emerald-500/20'
                }`}
                disabled={isSubmitting}
              />
              {errors.levels && <p className="mt-1 text-xs text-red-600">{errors.levels}</p>}
            </div>
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium text-slate-700">توضیحات (اختیاری)</label>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500"
              rows={2}
              disabled={isSubmitting}
            />
          </div>

          <div className="rounded-lg border border-slate-200 bg-slate-50 p-3 text-sm text-slate-700">
            <div>پیش‌نمایش نام/کد: <span className="font-mono text-slate-900">{(prefix || 'PREFIX')}-A01{Number(levels) > 1 ? '-L1' : ''}</span></div>
            <div className="mt-1 text-xs text-slate-500">تعداد نهایی قفسه‌ها: {isNaN(total) ? 0 : total}</div>
          </div>

          <div className="flex items-center justify-end gap-3 border-t border-slate-200 pt-4">
            <button
              type="button"
              onClick={handleClose}
              disabled={isSubmitting}
              className="rounded-lg px-4 py-2.5 text-sm font-medium text-slate-600 transition-colors hover:bg-slate-100 disabled:opacity-50"
            >
              انصراف
            </button>
            <button
              type="submit"
              disabled={isSubmitting || loadingWarehouses}
              className="inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-5 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-emerald-700 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isSubmitting ? (
                <>
                  <Spinner className="h-4 w-4 text-white" />
                  در حال ایجاد...
                </>
              ) : (
                <>
                  <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                  </svg>
                  ایجاد قفسه‌ها
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}


