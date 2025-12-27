import { useEffect, useMemo, useState, type FormEvent } from 'react'
import type { ReceiptRejectionListItem } from '../types'

interface ResolveReceiptRejectionModalProps {
  isOpen: boolean
  item: ReceiptRejectionListItem | null
  onClose: () => void
  onSubmit: (payload: { approvedQty: number; returnedQty: number; disposedQty: number; note?: string }) => Promise<void>
  isSubmitting: boolean
}

export function ResolveReceiptRejectionModal({
  isOpen,
  item,
  onClose,
  onSubmit,
  isSubmitting,
}: ResolveReceiptRejectionModalProps) {
  const [approvedQty, setApprovedQty] = useState('')
  const [returnedQty, setReturnedQty] = useState('')
  const [disposedQty, setDisposedQty] = useState('')
  const [note, setNote] = useState('')
  const [error, setError] = useState('')

  const resolvedQty = useMemo(() => {
    if (!item) return 0
    return item.rejectionApprovedQty + item.rejectionReturnedQty + item.rejectionDisposedQty
  }, [item])

  const remainingQty = useMemo(() => {
    if (!item) return 0
    return item.rejectedQty - resolvedQty
  }, [item, resolvedQty])

  useEffect(() => {
    if (isOpen) {
      setApprovedQty('')
      setReturnedQty('')
      setDisposedQty('')
      setNote('')
      setError('')
    }
  }, [isOpen, item])

  if (!isOpen || !item) return null

  const parseQty = (value: string) => {
    const parsed = parseFloat(value)
    return Number.isFinite(parsed) ? parsed : 0
  }

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    setError('')

    const approved = parseQty(approvedQty)
    const returned = parseQty(returnedQty)
    const disposed = parseQty(disposedQty)
    const total = approved + returned + disposed

    if (approved < 0 || returned < 0 || disposed < 0) {
      setError('مقادیر نمی‌توانند منفی باشند.')
      return
    }

    if (total <= 0) {
      setError('حداقل یکی از مقادیر باید بزرگتر از صفر باشد.')
      return
    }

    if (total > remainingQty) {
      setError('جمع مقادیر نمی‌تواند از باقیمانده بیشتر باشد.')
      return
    }

    try {
      await onSubmit({
        approvedQty: approved,
        returnedQty: returned,
        disposedQty: disposed,
        note: note.trim() || undefined,
      })
      onClose()
    } catch (err: any) {
      setError(err.message || 'خطا در ثبت تعیین تکلیف')
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
      <div className="w-full max-w-lg rounded-xl bg-white shadow-xl">
        <div className="border-b border-slate-200 px-6 py-4">
          <h3 className="text-lg font-semibold text-slate-900">تعیین تکلیف اقلام رد شده</h3>
          <p className="mt-1 text-sm text-slate-500">
            مقادیر تایید، مرجوعی یا معدومی را برای این خط مشخص کنید.
          </p>
        </div>

        <form onSubmit={handleSubmit} className="px-6 py-4">
          <div className="space-y-4">
            <div className="rounded-lg border border-slate-200 bg-slate-50 p-3 text-sm">
              <div className="flex items-center justify-between">
                <span className="text-slate-600">مقدار رد شده:</span>
                <span className="font-medium text-slate-900">{item.rejectedQty.toLocaleString('fa-IR')}</span>
              </div>
              <div className="mt-2 flex items-center justify-between">
                <span className="text-slate-600">رسیدگی شده:</span>
                <span className="font-medium text-slate-700">{resolvedQty.toLocaleString('fa-IR')}</span>
              </div>
              <div className="mt-2 flex items-center justify-between border-t border-slate-200 pt-2">
                <span className="font-medium text-slate-700">باقیمانده:</span>
                <span className="font-bold text-emerald-600">{remainingQty.toLocaleString('fa-IR')}</span>
              </div>
            </div>

            <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
              <div>
                <label className="mb-2 block text-sm font-medium text-slate-700">تایید به انبار</label>
                <input
                  type="number"
                  value={approvedQty}
                  onChange={(e) => setApprovedQty(e.target.value)}
                  min="0"
                  step="0.01"
                  disabled={isSubmitting}
                  className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20 disabled:bg-slate-100"
                />
              </div>
              <div>
                <label className="mb-2 block text-sm font-medium text-slate-700">مرجوعی</label>
                <input
                  type="number"
                  value={returnedQty}
                  onChange={(e) => setReturnedQty(e.target.value)}
                  min="0"
                  step="0.01"
                  disabled={isSubmitting}
                  className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20 disabled:bg-slate-100"
                />
              </div>
              <div>
                <label className="mb-2 block text-sm font-medium text-slate-700">معدومی</label>
                <input
                  type="number"
                  value={disposedQty}
                  onChange={(e) => setDisposedQty(e.target.value)}
                  min="0"
                  step="0.01"
                  disabled={isSubmitting}
                  className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20 disabled:bg-slate-100"
                />
              </div>
            </div>

            <div>
              <label className="mb-2 block text-sm font-medium text-slate-700">یادداشت</label>
              <textarea
                value={note}
                onChange={(e) => setNote(e.target.value)}
                disabled={isSubmitting}
                rows={3}
                className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20 disabled:bg-slate-100"
              />
            </div>

            {error && <p className="text-sm text-red-600">{error}</p>}
          </div>

          <div className="mt-6 flex items-center justify-end gap-3">
            <button
              type="button"
              onClick={onClose}
              disabled={isSubmitting}
              className="rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-50 disabled:opacity-50"
            >
              بستن
            </button>
            <button
              type="submit"
              disabled={isSubmitting || remainingQty <= 0}
              className="rounded-lg bg-emerald-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-emerald-700 disabled:opacity-50"
            >
              {isSubmitting ? 'در حال ثبت...' : 'ثبت تعیین تکلیف'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
