import { useState, useEffect, type FormEvent } from 'react'
import type { ReceiptLine } from '../types'

interface ApproveRejectLineModalProps {
  isOpen: boolean
  line: ReceiptLine | null
  mode: 'approve' | 'reject'
  onClose: () => void
  onSubmit: (qty: number, reason?: string) => Promise<void>
  isSubmitting: boolean
}

export function ApproveRejectLineModal({
  isOpen,
  line,
  mode,
  onClose,
  onSubmit,
  isSubmitting,
}: ApproveRejectLineModalProps) {
  const [qty, setQty] = useState('')
  const [reason, setReason] = useState('')
  const [error, setError] = useState('')

  const remainingQty = line?.remainingQty ?? 0
  const currentQty = line ? (mode === 'approve' ? line.approvedQty : line.rejectedQty) : 0
  // حداکثر مقدار قابل تایید/رد: مقدار کل خط
  const maxQty = line?.qty ?? 0

  // مقدار اولیه را با مقدار فعلی پر می‌کنیم
  useEffect(() => {
    if (isOpen && line) {
      const current = mode === 'approve' ? line.approvedQty : line.rejectedQty
      setQty(current.toString())
      setError('')
    }
  }, [isOpen, line, mode])

  if (!isOpen || !line) return null

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    setError('')

    const qtyNum = parseFloat(qty)
    if (isNaN(qtyNum) || qtyNum < 0) {
      setError('مقدار باید بیشتر یا مساوی صفر باشد')
      return
    }

    if (qtyNum > maxQty) {
      setError(`مقدار نمی‌تواند بیشتر از ${maxQty.toLocaleString('fa-IR')} باشد`)
      return
    }

    // بررسی اینکه مجموع تایید شده و رد شده از مقدار کل بیشتر نشود
    const otherQty = mode === 'approve' ? line.rejectedQty : line.approvedQty
    if (qtyNum + otherQty > maxQty) {
      setError(`مجموع تایید شده و رد شده نمی‌تواند از مقدار کل (${maxQty.toLocaleString('fa-IR')}) بیشتر باشد`)
      return
    }

    // اگر مقدار تغییر نکرده، نیازی به ارسال نیست
    if (qtyNum === currentQty) {
      onClose()
      return
    }

    try {
      // فقط برای رد کردن، اگر مقدار افزایش یافته، دلیل لازم است
      if (mode === 'reject' && qtyNum > currentQty && !reason.trim()) {
        setError('لطفاً دلیل رد را وارد کنید')
        return
      }

      // ارسال مقدار کل جدید (backend خودش محاسبه می‌کند که چقدر تغییر کرده)
      await onSubmit(qtyNum, mode === 'reject' ? reason.trim() : undefined)
      setQty('')
      setReason('')
      onClose()
    } catch (err: any) {
      setError(err.message || 'خطا در انجام عملیات')
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
      <div className="w-full max-w-md rounded-xl bg-white shadow-xl">
        <div className="border-b border-slate-200 px-6 py-4">
          <h3 className="text-lg font-semibold text-slate-900">
            {mode === 'approve' ? 'ویرایش مقدار تایید شده' : 'ویرایش مقدار رد شده'}
          </h3>
          <p className="mt-1 text-sm text-slate-500">
            می‌توانید مقدار را کم یا زیاد کنید. فقط توجه داشته باشید که مجموع تایید شده و رد شده نباید از مقدار کل بیشتر شود.
          </p>
        </div>

        <form onSubmit={handleSubmit} className="px-6 py-4">
          <div className="space-y-4">
            <div>
              <label className="mb-2 block text-sm font-medium text-slate-700">
                خط {line.lineNo}
              </label>
              <div className="rounded-lg border border-slate-200 bg-slate-50 p-3 text-sm">
                <div className="flex items-center justify-between">
                  <span className="text-slate-600">مقدار کل:</span>
                  <span className="font-medium text-slate-900">{line.qty.toLocaleString('fa-IR')}</span>
                </div>
                <div className="mt-2 flex items-center justify-between">
                  <span className="text-slate-600">تایید شده:</span>
                  <span className="font-medium text-green-600">{line.approvedQty.toLocaleString('fa-IR')}</span>
                </div>
                <div className="mt-2 flex items-center justify-between">
                  <span className="text-slate-600">رد شده:</span>
                  <span className="font-medium text-red-600">{line.rejectedQty.toLocaleString('fa-IR')}</span>
                </div>
                <div className="mt-2 flex items-center justify-between border-t border-slate-200 pt-2">
                  <span className="font-medium text-slate-700">باقیمانده:</span>
                  <span className="font-bold text-blue-600">{remainingQty.toLocaleString('fa-IR')}</span>
                </div>
              </div>
            </div>

            <div>
              <label className="mb-2 block text-sm font-medium text-slate-700">
                مقدار کل {mode === 'approve' ? 'تایید شده' : 'رد شده'} <span className="text-red-500">*</span>
              </label>
              <input
                type="number"
                value={qty || currentQty.toString()}
                onChange={(e) => {
                  setQty(e.target.value)
                  setError('')
                }}
                min="0"
                max={maxQty}
                step="0.01"
                required
                disabled={isSubmitting}
                className={`w-full rounded-lg border px-3 py-2.5 text-sm transition-colors focus:outline-none focus:ring-2 ${
                  error
                    ? 'border-red-300 focus:border-red-500 focus:ring-red-500/20'
                    : 'border-slate-300 focus:border-emerald-500 focus:ring-emerald-500/20'
                } disabled:bg-slate-100 disabled:cursor-not-allowed`}
                placeholder={`مقدار فعلی: ${currentQty.toLocaleString('fa-IR')} - حداکثر: ${maxQty.toLocaleString('fa-IR')}`}
              />
              <p className="mt-1 text-xs text-slate-500">
                مقدار فعلی: <span className="font-medium">{currentQty.toLocaleString('fa-IR')}</span> | 
                {' '}تغییر: <span className={`font-medium ${qty && parseFloat(qty) !== currentQty ? (parseFloat(qty) > currentQty ? 'text-green-600' : 'text-red-600') : 'text-slate-500'}`}>
                  {qty ? (parseFloat(qty) - currentQty).toLocaleString('fa-IR') : '0'}
                </span>
              </p>
              {error && <p className="mt-1 text-xs text-red-600">{error}</p>}
            </div>

            {mode === 'reject' && (
              <div>
                <label className="mb-2 block text-sm font-medium text-slate-700">
                  دلیل رد {qty && parseFloat(qty) > currentQty && <span className="text-red-500">*</span>}
                </label>
                <textarea
                  value={reason}
                  onChange={(e) => {
                    setReason(e.target.value)
                    setError('')
                  }}
                  required={qty ? parseFloat(qty) > currentQty : false}
                  disabled={isSubmitting}
                  rows={3}
                  className={`w-full rounded-lg border px-3 py-2.5 text-sm transition-colors focus:outline-none focus:ring-2 ${
                    error
                      ? 'border-red-300 focus:border-red-500 focus:ring-red-500/20'
                      : 'border-slate-300 focus:border-emerald-500 focus:ring-emerald-500/20'
                  } disabled:bg-slate-100 disabled:cursor-not-allowed`}
                  placeholder="لطفاً دلیل رد را وارد کنید (فقط در صورت افزایش مقدار رد شده الزامی است)..."
                />
                <p className="mt-1 text-xs text-slate-500">
                  {qty && parseFloat(qty) > currentQty
                    ? 'دلیل رد الزامی است'
                    : 'دلیل رد فقط در صورت افزایش مقدار رد شده الزامی است'}
                </p>
              </div>
            )}
          </div>

          <div className="mt-6 flex items-center justify-end gap-3">
            <button
              type="button"
              onClick={onClose}
              disabled={isSubmitting}
              className="rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-50 disabled:opacity-50"
            >
              انصراف
            </button>
            <button
              type="submit"
              disabled={isSubmitting}
              className={`rounded-lg px-4 py-2 text-sm font-medium text-white transition-colors disabled:opacity-50 ${
                mode === 'approve'
                  ? 'bg-emerald-600 hover:bg-emerald-700'
                  : 'bg-red-600 hover:bg-red-700'
              }`}
            >
              {isSubmitting ? 'در حال انجام...' : mode === 'approve' ? 'تایید' : 'رد کردن'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

