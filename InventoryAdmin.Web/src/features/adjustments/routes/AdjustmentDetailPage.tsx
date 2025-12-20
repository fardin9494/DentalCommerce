import { useState, useMemo } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { PageHeader } from '@/shared/components/PageHeader'
import { Spinner } from '@/shared/components/Spinner'
import {
  useAdjustment,
  useAddAdjustmentLine,
  useRemoveAdjustmentLine,
  useUpdateAdjustmentHeader,
  useUpdateAdjustmentLine,
  usePostAdjustment,
  useCancelAdjustment,
} from '../queries'
import { useConfirm } from '@/shared/components/confirm/ConfirmProvider'
import { swalPrompt } from '@/shared/utils/swal'
import { AddAdjustmentLineModal } from '../components/AddAdjustmentLineModal'
import { EditAdjustmentLineModal } from '../components/EditAdjustmentLineModal'
import type { AdjustmentLine } from '../types'
import {
  AdjustmentStatusLabels,
  AdjustmentReasonLabels,
  AdjustmentStatusColors,
  type AdjustmentStatus,
  type AdjustmentReason,
} from '../types'
import { useProductNames } from '@/shared/hooks/useProductNames'
import { adjustmentDisplayNote } from '@/shared/utils/inventoryDocumentReference'

export function AdjustmentDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { data: adjustment, isLoading, error } = useAdjustment(id)
  const addLine = useAddAdjustmentLine()
  const removeLine = useRemoveAdjustmentLine()
  const updateHeader = useUpdateAdjustmentHeader()
  const updateLine = useUpdateAdjustmentLine()
  const post = usePostAdjustment()
  const cancel = useCancelAdjustment()
  const confirm = useConfirm()
  const [showAddLineModal, setShowAddLineModal] = useState(false)
  const [editingLine, setEditingLine] = useState<AdjustmentLine | null>(null)

  // جمع‌آوری productId های تمام خطوط برای fetch کردن نام محصولات
  const productIds = useMemo(() => adjustment?.lines.map((line) => line.productId) || [], [adjustment?.lines])
  const { getProductName, getVariantName } = useProductNames(productIds)

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-20">
        <Spinner className="h-8 w-8 text-emerald-600" />
      </div>
    )
  }

  if (error || !adjustment) {
    return (
      <div className="rounded-xl border border-red-200 bg-red-50 p-8 text-center">
        <div className="mx-auto mb-4 h-12 w-12 rounded-full bg-red-100 p-3 text-red-600">
          <svg fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126ZM12 15.75h.007v.008H12v-.008Z"
            />
          </svg>
        </div>
        <p className="text-lg font-medium text-red-800">اصلاح موجودی پیدا نشد</p>
        <p className="mt-2 text-sm text-red-600">
          {(error as Error)?.message || 'خطا در دریافت اطلاعات اصلاح موجودی'}
        </p>
        <button
          onClick={() => navigate('/adjustments')}
          className="mt-4 inline-flex items-center gap-2 rounded-lg bg-red-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-red-700"
        >
          <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" d="M9 15 3 9m0 0 6-6M3 9h12a6 6 0 0 1 0 12h-3" />
          </svg>
          بازگشت به لیست
        </button>
      </div>
    )
  }

  async function handleAddLine(data: {
    stockItemId: string
    productId: string
    variantId?: string
    lotNumber?: string
    expiryDateUtc?: string
    qtyDelta: number
  }) {
    await addLine.mutateAsync({
      adjustmentId: id!,
      dto: {
        stockItemId: data.stockItemId,
        productId: data.productId,
        variantId: data.variantId || null,
        lotNumber: data.lotNumber || null,
        expiryDateUtc: data.expiryDateUtc || null,
        qtyDelta: data.qtyDelta,
      },
    })
    setShowAddLineModal(false)
  }

  async function handleRemoveLine(lineId: string) {
    const ok = await confirm.confirm({ title: 'حذف خط', message: 'آیا از حذف این خط مطمئن هستید؟' })
    if (!ok) return
    await removeLine.mutateAsync({ adjustmentId: id!, lineId })
  }

  async function handleUpdateLine(data: { qtyDelta: number }) {
    if (!editingLine) return
    await updateLine.mutateAsync({
      adjustmentId: id!,
      lineId: editingLine.id,
      dto: data,
    })
    setEditingLine(null)
  }

  async function handleUpdateHeader() {
    const note = await swalPrompt({
      title: 'ویرایش هدر اصلاح',
      inputLabel: 'یادداشت',
      defaultValue: adjustment?.note || '',
      required: false,
    })
    const docDate = await swalPrompt({
      title: 'تاریخ سند',
      inputLabel: 'تاریخ سند (ISO)',
      defaultValue: adjustment?.docDate ? new Date(adjustment.docDate).toISOString().split('T')[0] : '',
      required: false,
    })
    await updateHeader.mutateAsync({
      adjustmentId: id!,
      dto: {
        note: note || null,
        docDateUtc: docDate ? new Date(docDate).toISOString() : null,
      },
    })
  }

  async function handlePost() {
    const ok = await confirm.confirm({
      title: 'ثبت اصلاح موجودی',
      message: 'آیا می‌خواهید این اصلاح موجودی را ثبت کنید؟ تغییرات روی موجودی انبار اعمال خواهد شد.',
    })
    if (!ok) return
    await post.mutateAsync(id!)
  }

  async function handleCancel() {
    const ok = await confirm.confirm({
      title: 'لغو اصلاح موجودی',
      message: 'آیا می‌خواهید این اصلاح موجودی را لغو کنید؟ این عملیات قابل بازگشت نیست.',
    })
    if (!ok) return
    await cancel.mutateAsync(id!)
  }

  function formatDate(dateStr: string) {
    try {
      return new Date(dateStr).toLocaleDateString('fa-IR', {
        year: 'numeric',
        month: 'long',
        day: 'numeric',
      })
    } catch {
      return dateStr
    }
  }

  function formatDateTime(dateStr: string | null | undefined) {
    if (!dateStr) return '-'
    try {
      return new Date(dateStr).toLocaleDateString('fa-IR', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      })
    } catch {
      return dateStr
    }
  }

  const isDraft = adjustment.status === 'Draft'
  const totalQtyDelta = adjustment.lines.reduce((sum, line) => sum + line.qtyDelta, 0)

  return (
    <div className="space-y-6">
      {/* Header */}
      <PageHeader
        title={`اصلاح موجودی #${adjustment.id.substring(0, 8)}`}
        actions={
          <div className="flex flex-wrap items-center gap-2">
            {isDraft && (
              <>
                <button
                  onClick={() => setShowAddLineModal(true)}
                  className="inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-4 py-2 text-sm font-medium text-white shadow-sm transition-colors hover:bg-emerald-700"
                >
                  <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                  </svg>
                  افزودن خط
                </button>
                <button
                  onClick={handleUpdateHeader}
                  disabled={updateHeader.isPending}
                  className="inline-flex items-center gap-2 rounded-lg bg-slate-100 px-4 py-2 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-200 disabled:opacity-50"
                >
                  <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      d="m16.862 4.487 1.687-1.688a1.875 1.875 0 1 1 2.652 2.652L10.582 16.07a4.5 4.5 0 0 1-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 0 1 1.13-1.897l8.932-8.931Z"
                    />
                  </svg>
                  ویرایش
                </button>
                <button
                  onClick={handlePost}
                  disabled={post.isPending || adjustment.lines.length === 0}
                  className="inline-flex items-center gap-2 rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white shadow-sm transition-colors hover:bg-blue-700 disabled:opacity-50"
                >
                  <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" d="m4.5 12.75 6 6 9-13.5" />
                  </svg>
                  ثبت
                </button>
                <button
                  onClick={handleCancel}
                  disabled={cancel.isPending}
                  className="inline-flex items-center gap-2 rounded-lg bg-red-100 px-4 py-2 text-sm font-medium text-red-700 transition-colors hover:bg-red-200 disabled:opacity-50"
                >
                  <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M6 18 18 6M6 6l12 12" />
                  </svg>
                  لغو
                </button>
              </>
            )}
            <button
              onClick={() => navigate('/adjustments')}
              className="inline-flex items-center gap-2 rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-50"
            >
              <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" d="M9 15 3 9m0 0 6-6M3 9h12a6 6 0 0 1 0 12h-3" />
              </svg>
              بازگشت
            </button>
          </div>
        }
      >
        مدیریت اصلاح موجودی و انبارگردانی
      </PageHeader>

      {/* Info Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        {/* Status Card */}
        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
          <div className="text-sm font-medium text-slate-500">وضعیت</div>
          <div className="mt-2">
            <span
              className={`inline-flex items-center rounded-full px-3 py-1 text-sm font-medium ${
                AdjustmentStatusColors[adjustment.status as AdjustmentStatus] || 'bg-slate-100 text-slate-700'
              }`}
            >
              {AdjustmentStatusLabels[adjustment.status as AdjustmentStatus] || adjustment.status}
            </span>
          </div>
        </div>

        {/* Reason Card */}
        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
          <div className="text-sm font-medium text-slate-500">دلیل اصلاح</div>
          <div className="mt-2">
            <span className="inline-flex items-center rounded-full bg-slate-100 px-3 py-1 text-sm font-medium text-slate-700">
              {AdjustmentReasonLabels[adjustment.reason as AdjustmentReason] || adjustment.reason}
            </span>
          </div>
        </div>

        {/* Lines Count Card */}
        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
          <div className="text-sm font-medium text-slate-500">تعداد خطوط</div>
          <div className="mt-2 text-2xl font-bold text-slate-900">{adjustment.lines.length}</div>
        </div>

        {/* Total Qty Delta Card */}
        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
          <div className="text-sm font-medium text-slate-500">مجموع تغییر</div>
          <div className="mt-2">
            <span
              className={`text-2xl font-bold ${
                totalQtyDelta > 0
                  ? 'text-emerald-600'
                  : totalQtyDelta < 0
                    ? 'text-red-600'
                    : 'text-slate-600'
              }`}
            >
              {totalQtyDelta > 0 ? '+' : ''}
              {totalQtyDelta.toLocaleString('fa-IR')}
            </span>
          </div>
        </div>
      </div>

      {/* Details */}
      <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
        <h3 className="mb-4 text-lg font-semibold text-slate-900">اطلاعات سند</h3>
        <div className="grid gap-4 md:grid-cols-2">
          <div>
            <div className="text-sm font-medium text-slate-500">تاریخ سند</div>
            <div className="mt-1 text-slate-900">{formatDate(adjustment.docDate)}</div>
          </div>
          {adjustment.postedAt && (
            <div>
              <div className="text-sm font-medium text-slate-500">تاریخ ثبت</div>
              <div className="mt-1 text-slate-900">{formatDateTime(adjustment.postedAt)}</div>
            </div>
          )}
          {(adjustment.note?.trim() || ' ') && (
            <div className="md:col-span-2">
              <div className="text-sm font-medium text-slate-500">یادداشت</div>
              <div className="mt-1 text-slate-900">{adjustmentDisplayNote(adjustment.id, adjustment.reason, adjustment.note)}</div>
            </div>
          )}
        </div>
      </div>

      {/* Lines Table */}
      <div className="rounded-xl border border-slate-200 bg-white shadow-sm overflow-hidden">
        <div className="border-b border-slate-200 bg-slate-50 px-6 py-4">
          <h3 className="text-lg font-semibold text-slate-900">خطوط اصلاح</h3>
        </div>
        {adjustment.lines.length === 0 ? (
          <div className="py-12 text-center">
            <p className="text-slate-500">هیچ خطی اضافه نشده است</p>
            {isDraft && (
              <button
                onClick={() => setShowAddLineModal(true)}
                className="mt-4 inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-emerald-700"
              >
                افزودن خط اول
              </button>
            )}
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-slate-200 bg-slate-50 text-right">
                  <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">ردیف</th>
                  <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">محصول</th>
                  <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">لات</th>
                  <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">انقضا</th>
                  <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600 text-center">تغییر موجودی</th>
                  {isDraft && <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">عملیات</th>}
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {adjustment.lines.map((line) => (
                  <tr key={line.id} className="transition-colors hover:bg-slate-50">
                    <td className="whitespace-nowrap px-4 py-3 text-slate-700">{line.lineNo}</td>
                    <td className="px-4 py-3">
                      <div className="font-medium text-slate-900">{getProductName(line.productId)}</div>
                      {line.variantId && (
                        <div className="mt-0.5 text-sm text-emerald-600">
                          واریانت: {getVariantName(line.variantId) || line.variantId.substring(0, 8) + '...'}
                        </div>
                      )}
                    </td>
                    <td className="whitespace-nowrap px-4 py-3 text-slate-600">{line.lotNumber || '-'}</td>
                    <td className="whitespace-nowrap px-4 py-3 text-slate-600">
                      {line.expiryDateUtc ? formatDate(line.expiryDateUtc) : '-'}
                    </td>
                    <td className="whitespace-nowrap px-4 py-3 text-center">
                      <span
                        className={`font-medium ${
                          line.qtyDelta > 0
                            ? 'text-emerald-600'
                            : line.qtyDelta < 0
                              ? 'text-red-600'
                              : 'text-slate-600'
                        }`}
                      >
                        {line.qtyDelta > 0 ? '+' : ''}
                        {line.qtyDelta.toLocaleString('fa-IR')}
                      </span>
                    </td>
                    {isDraft && (
                      <td className="whitespace-nowrap px-4 py-3">
                        <div className="flex items-center gap-2">
                          <button
                            onClick={() => setEditingLine(line)}
                            className="inline-flex items-center gap-1 rounded-lg bg-slate-100 px-2.5 py-1.5 text-xs font-medium text-slate-700 transition-colors hover:bg-slate-200"
                          >
                            ویرایش
                          </button>
                          <button
                            onClick={() => handleRemoveLine(line.id)}
                            className="inline-flex items-center gap-1 rounded-lg bg-red-100 px-2.5 py-1.5 text-xs font-medium text-red-700 transition-colors hover:bg-red-200"
                          >
                            حذف
                          </button>
                        </div>
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Modals */}
      <AddAdjustmentLineModal
        isOpen={showAddLineModal}
        warehouseId={adjustment.warehouseId}
        onClose={() => setShowAddLineModal(false)}
        onSubmit={handleAddLine}
        isSubmitting={addLine.isPending}
      />

      {editingLine && (
        <EditAdjustmentLineModal
          isOpen={!!editingLine}
          line={editingLine}
          onClose={() => setEditingLine(null)}
          onSubmit={handleUpdateLine}
          isSubmitting={updateLine.isPending}
        />
      )}
    </div>
  )
}
