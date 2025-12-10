import { useState, useMemo } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { PageHeader } from '@/shared/components/PageHeader'
import { Spinner } from '@/shared/components/Spinner'
import {
  useReceipt,
  useAddReceiptLine,
  useRemoveReceiptLine,
  useUpdateReceiptHeader,
  useUpdateReceiptLine,
  useReceiveReceipt,
  useApproveReceipt,
  useCancelReceipt,
  useApproveReceiptLinePartial,
  useRejectReceiptLine,
} from '../queries'
import { useConfirm } from '@/shared/components/confirm/ConfirmProvider'
import { swalPrompt } from '@/shared/utils/swal'
import { AddReceiptLineModal } from '../components/AddReceiptLineModal'
import { EditReceiptLineModal } from '../components/EditReceiptLineModal'
import { ApproveRejectLineModal } from '../components/ApproveRejectLineModal'
import type { ReceiptLine } from '../types'
import {
  ReceiptStatusLabels,
  ReceiptReasonLabels,
  ReceiptStatusColors,
  type ReceiptStatus,
  type ReceiptReason,
} from '../types'
import { useProductNames } from '@/shared/hooks/useProductNames'
import { useWarehouseNames } from '@/shared/hooks/useWarehouses'

export function ReceiptDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { data: receipt, isLoading, error } = useReceipt(id)
  const addLine = useAddReceiptLine(id!)
  const removeLine = useRemoveReceiptLine(id!)
  const updateHeader = useUpdateReceiptHeader(id!)
  const receive = useReceiveReceipt(id!)
  const approve = useApproveReceipt(id!)
  const cancel = useCancelReceipt(id!)
  const confirm = useConfirm()
  const [showAddLineModal, setShowAddLineModal] = useState(false)
  const [editingLine, setEditingLine] = useState<ReceiptLine | null>(null)
  const [approveRejectLine, setApproveRejectLine] = useState<{ line: ReceiptLine; mode: 'approve' | 'reject' } | null>(null)
  const updateLine = useUpdateReceiptLine(id!)
  const approveLinePartial = useApproveReceiptLinePartial(id!)
  const rejectLine = useRejectReceiptLine(id!)

  // جمع‌آوری productId های تمام خطوط (در صورت وجود) برای دریافت نام محصولات
  const productIds = useMemo(() => receipt?.lines.map((line) => line.productId) || [], [receipt?.lines])
  const { getProductName, getVariantName } = useProductNames(productIds)
  const { getWarehouseName } = useWarehouseNames()

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-20">
        <Spinner className="h-8 w-8 text-emerald-600" />
      </div>
    )
  }

  if (error || !receipt) {
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
        <p className="text-lg font-medium text-red-800">رسید پیدا نشد</p>
        <p className="mt-2 text-sm text-red-600">{(error as Error)?.message || 'خطا در دریافت اطلاعات رسید'}</p>
        <button
          onClick={() => navigate('/receipts')}
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
    productId: string
    variantId?: string
    qty: number
    lotNumber?: string
    expiryDateUtc?: string
    unitCost?: number
  }) {
    await addLine.mutateAsync({
      productId: data.productId,
      variantId: data.variantId || null,
      qty: data.qty,
      lotNumber: data.lotNumber || null,
      expiryDateUtc: data.expiryDateUtc || null,
      unitCost: data.unitCost || null,
    })
    setShowAddLineModal(false)
  }

  async function handleRemoveLine(lineId: string) {
    const ok = await confirm.confirm({ title: 'حذف خط', message: 'آیا از حذف این خط مطمئن هستید؟' })
    if (!ok) return
    await removeLine.mutateAsync(lineId)
  }

  async function handleUpdateLine(data: {
    qty?: number
    lotNumber?: string | null
    expiryDateUtc?: string | null
    unitCost?: number | null
  }) {
    if (!editingLine) return
    await updateLine.mutateAsync({
      lineId: editingLine.id,
      dto: data,
    })
    setEditingLine(null)
  }

  async function handleUpdateHeader() {
    const externalRef = await swalPrompt({
      title: 'ویرایش هدر رسید',
      inputLabel: 'شماره مرجع خارجی',
      defaultValue: receipt?.externalRef || '',
      required: false,
    })
    const docDate = await swalPrompt({
      title: 'تاریخ سند',
      inputLabel: 'تاریخ سند (ISO)',
      defaultValue: receipt?.docDate ? new Date(receipt.docDate).toISOString().split('T')[0] : '',
      required: false,
    })
    await updateHeader.mutateAsync({
      externalRef: externalRef || null,
      docDateUtc: docDate ? new Date(docDate).toISOString() : null,
    })
  }

  async function handleReceive() {
    const ok = await confirm.confirm({
      title: 'دریافت رسید',
      message: 'آیا می‌خواهید این رسید را دریافت کنید؟ کالاها وارد انبار خواهند شد.',
    })
    if (!ok) return
    await receive.mutateAsync()
  }

  async function handleApprove() {
    const ok = await confirm.confirm({
      title: 'تایید نهایی رسید',
      message: 'آیا از تایید نهایی این رسید مطمئن هستید؟ فقط مقادیر تایید شده برای فروش در دسترس خواهند بود.',
    })
    if (!ok) return
    await approve.mutateAsync()
  }

  async function handleCancel() {
    const ok = await confirm.confirm({
      title: 'لغو رسید',
      message: 'آیا می‌خواهید این رسید را لغو کنید؟ این عملیات قابل بازگشت نیست.',
    })
    if (!ok) return
    await cancel.mutateAsync()
  }

  async function handleApproveRejectLine(qty: number, reason?: string) {
    if (!approveRejectLine) return
    if (approveRejectLine.mode === 'approve') {
      await approveLinePartial.mutateAsync({ lineId: approveRejectLine.line.id, qty })
    } else {
      await rejectLine.mutateAsync({ lineId: approveRejectLine.line.id, qty, reason })
    }
    setApproveRejectLine(null)
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

  const isDraft = receipt.status === 'Draft'
  const isReceived = receipt.status === 'Received'
  const isApproved = receipt.status === 'Approved'
  
  // بررسی اینکه آیا همه خطوط تکلیفشان مشخص شده است
  const allLinesCompleted = isReceived && receipt.lines.every((line) => line.remainingQty === 0)

  return (
    <div className="space-y-6">
      {/* Header */}
      <PageHeader
        title={`رسید #${receipt.id.substring(0, 8)}`}
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
                  onClick={handleReceive}
                  disabled={receive.isPending || receipt.lines.length === 0}
                  className="inline-flex items-center gap-2 rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white shadow-sm transition-colors hover:bg-blue-700 disabled:opacity-50"
                >
                  <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" d="m4.5 12.75 6 6 9-13.5" />
                  </svg>
                  دریافت
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
            {isReceived && allLinesCompleted && (
              <button
                onClick={handleApprove}
                disabled={approve.isPending}
                className="inline-flex items-center gap-2 rounded-lg bg-green-600 px-4 py-2 text-sm font-medium text-white shadow-sm transition-colors hover:bg-green-700 disabled:opacity-50"
                title="تایید نهایی رسید - همه خطوط تکلیفشان مشخص شده است"
              >
                <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75 11.25 15 15 9.75M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
                </svg>
                تایید نهایی رسید
              </button>
            )}
            <button
              onClick={() => navigate('/receipts')}
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
        مدیریت رسید ورود به انبار
      </PageHeader>

      {/* Info Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        {/* Status Card */}
        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
          <div className="text-sm font-medium text-slate-500">وضعیت</div>
          <div className="mt-2">
            <span
              className={`inline-flex items-center rounded-full px-3 py-1 text-sm font-medium ${
                ReceiptStatusColors[receipt.status as ReceiptStatus] || 'bg-slate-100 text-slate-700'
              }`}
            >
              {ReceiptStatusLabels[receipt.status as ReceiptStatus] || receipt.status}
            </span>
          </div>
        </div>

        {/* Reason Card */}
        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
          <div className="text-sm font-medium text-slate-500">نوع رسید</div>
          <div className="mt-2">
            <span className="inline-flex items-center rounded-full bg-slate-100 px-3 py-1 text-sm font-medium text-slate-700">
              {ReceiptReasonLabels[receipt.reason as ReceiptReason] || receipt.reason}
            </span>
          </div>
        </div>

        {/* Doc Date Card */}
        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
          <div className="text-sm font-medium text-slate-500">تاریخ سند</div>
          <div className="mt-2 text-lg font-semibold text-slate-900">{formatDate(receipt.docDate)}</div>
        </div>

        {/* External Ref Card */}
        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
          <div className="text-sm font-medium text-slate-500">شماره مرجع</div>
          <div className="mt-2 text-lg font-semibold text-slate-900">{receipt.externalRef || '-'}</div>
        </div>
      </div>

      {/* Details Card */}
      <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
        <h3 className="mb-4 text-lg font-semibold text-slate-900">اطلاعات تکمیلی</h3>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <div>
            <div className="text-sm text-slate-500">شناسه رسید</div>
            <div className="mt-1 font-mono text-sm text-slate-700">{receipt.id}</div>
          </div>
          <div>
            <div className="text-sm text-slate-500">انبار</div>
            <div className="mt-1 text-sm font-semibold text-slate-900">{getWarehouseName(receipt.warehouseId)}</div>
          </div>
          <div>
            <div className="text-sm text-slate-500">تاریخ دریافت</div>
            <div className="mt-1 text-sm text-slate-700">{formatDateTime(receipt.receivedAt)}</div>
          </div>
          <div>
            <div className="text-sm text-slate-500">تاریخ تایید</div>
            <div className="mt-1 text-sm text-slate-700">{formatDateTime(receipt.approvedAt)}</div>
          </div>
        </div>
      </div>

      {/* Lines Table */}
      <div className="rounded-xl border border-slate-200 bg-white shadow-sm overflow-hidden">
        <div className="flex items-center justify-between border-b border-slate-200 bg-slate-50 px-6 py-4">
          <h3 className="text-lg font-semibold text-slate-900">
            خطوط رسید
            <span className="mr-2 rounded-full bg-slate-200 px-2.5 py-0.5 text-sm font-medium text-slate-600">
              {receipt.lines.length}
            </span>
          </h3>
          {isDraft && (
            <button
              onClick={() => setShowAddLineModal(true)}
              className="inline-flex items-center gap-1.5 rounded-lg bg-emerald-600 px-3 py-1.5 text-sm font-medium text-white transition-colors hover:bg-emerald-700"
            >
              <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
              </svg>
              افزودن
            </button>
          )}
        </div>

        {receipt.lines.length === 0 ? (
          <div className="py-12 text-center">
            <div className="mx-auto mb-4 h-12 w-12 rounded-full bg-slate-100 p-3 text-slate-400">
              <svg fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  d="M20.25 7.5l-.625 10.632a2.25 2.25 0 01-2.247 2.118H6.622a2.25 2.25 0 01-2.247-2.118L3.75 7.5M10 11.25h4M3.375 7.5h17.25c.621 0 1.125-.504 1.125-1.125v-1.5c0-.621-.504-1.125-1.125-1.125H3.375c-.621 0-1.125.504-1.125 1.125v1.5c0 .621.504 1.125 1.125 1.125z"
                />
              </svg>
            </div>
            <p className="text-slate-600">هیچ خطی اضافه نشده است</p>
            {isDraft && <p className="mt-1 text-sm text-slate-400">برای شروع یک خط جدید اضافه کنید</p>}
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-slate-200 bg-slate-50 text-right">
                  <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">ردیف</th>
                  <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">محصول</th>
                  <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">واریانت</th>
                  <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">تعداد</th>
                  <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">شماره لات</th>
                  <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">تاریخ انقضا</th>
                  <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">هزینه واحد</th>
                  {(isReceived || isApproved) && (
                    <>
                      <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">تایید شده</th>
                      <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">رد شده</th>
                      <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">باقیمانده</th>
                      {isReceived && <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">عملیات</th>}
                    </>
                  )}
                  {isDraft && <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">عملیات</th>}
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {receipt.lines.map((line) => (
                  <tr key={line.id} className="transition-colors hover:bg-slate-50">
                    <td className="whitespace-nowrap px-4 py-3 text-slate-700">{line.lineNo}</td>
                    <td className="whitespace-nowrap px-4 py-3">
                      <div className="font-medium text-slate-900">{getProductName(line.productId)}</div>
                    </td>
                    <td className="whitespace-nowrap px-4 py-3">
                      {line.variantId ? (
                        <div className="text-sm text-emerald-600">{getVariantName(line.variantId) || line.variantId.substring(0, 8) + '...'}</div>
                      ) : (
                        <span className="text-slate-400">-</span>
                      )}
                    </td>
                    <td className="whitespace-nowrap px-4 py-3 font-medium text-slate-900">
                      {line.qty.toLocaleString('fa-IR')}
                    </td>
                    <td className="whitespace-nowrap px-4 py-3 text-slate-600">{line.lotNumber || '-'}</td>
                    <td className="whitespace-nowrap px-4 py-3 text-slate-600">
                      {line.expiryDateUtc ? formatDate(line.expiryDateUtc) : '-'}
                    </td>
                    <td className="whitespace-nowrap px-4 py-3 text-slate-600">
                      {line.unitCost ? `${line.unitCost.toLocaleString('fa-IR')} ریال` : '-'}
                    </td>
                    {(isReceived || isApproved) && (
                      <>
                        <td className="whitespace-nowrap px-4 py-3">
                          <span className="inline-flex items-center rounded-full bg-green-100 px-2.5 py-0.5 text-xs font-medium text-green-700">
                            {line.approvedQty.toLocaleString('fa-IR')}
                          </span>
                        </td>
                        <td className="whitespace-nowrap px-4 py-3">
                          {line.rejectedQty > 0 ? (
                            <div>
                              <span className="inline-flex items-center rounded-full bg-red-100 px-2.5 py-0.5 text-xs font-medium text-red-700">
                                {line.rejectedQty.toLocaleString('fa-IR')}
                              </span>
                              {line.rejectionReason && (
                                <div className="mt-1 text-xs text-red-600" title={line.rejectionReason}>
                                  <span className="font-medium">دلیل:</span>{' '}
                                  {line.rejectionReason.length > 40
                                    ? `${line.rejectionReason.substring(0, 40)}...`
                                    : line.rejectionReason}
                                </div>
                              )}
                            </div>
                          ) : (
                            <span className="text-slate-400">-</span>
                          )}
                        </td>
                        <td className="whitespace-nowrap px-4 py-3">
                          <span
                            className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${
                              line.remainingQty > 0
                                ? 'bg-blue-100 text-blue-700'
                                : 'bg-slate-100 text-slate-600'
                            }`}
                          >
                            {line.remainingQty.toLocaleString('fa-IR')}
                          </span>
                        </td>
                        {isReceived && (
                          <td className="whitespace-nowrap px-4 py-3">
                            <div className="flex items-center gap-1">
                              <button
                                onClick={() => setApproveRejectLine({ line, mode: 'approve' })}
                                disabled={approveLinePartial.isPending || rejectLine.isPending}
                                className="rounded-lg p-1.5 text-emerald-600 transition-colors hover:bg-emerald-50 hover:text-emerald-700 disabled:opacity-50"
                                title="ویرایش مقدار تایید شده (می‌توانید کم یا زیاد کنید)"
                              >
                                <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
                                  <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75 11.25 15 15 9.75M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
                                </svg>
                              </button>
                              <button
                                onClick={() => setApproveRejectLine({ line, mode: 'reject' })}
                                disabled={approveLinePartial.isPending || rejectLine.isPending}
                                className="rounded-lg p-1.5 text-red-600 transition-colors hover:bg-red-50 hover:text-red-700 disabled:opacity-50"
                                title="ویرایش مقدار رد شده (می‌توانید کم یا زیاد کنید)"
                              >
                                <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
                                  <path strokeLinecap="round" strokeLinejoin="round" d="M6 18 18 6M6 6l12 12" />
                                </svg>
                              </button>
                            </div>
                          </td>
                        )}
                      </>
                    )}
                    {isDraft && (
                      <td className="whitespace-nowrap px-4 py-3">
                        <div className="flex items-center gap-1">
                          <button
                            onClick={() => setEditingLine(line)}
                            className="rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-600"
                            title="ویرایش"
                          >
                            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
                              <path strokeLinecap="round" strokeLinejoin="round" d="m16.862 4.487 1.687-1.688a1.875 1.875 0 1 1 2.652 2.652L10.582 16.07a4.5 4.5 0 0 1-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 0 1 1.13-1.897l8.932-8.931Z" />
                            </svg>
                          </button>
                          <button
                            onClick={() => handleRemoveLine(line.id)}
                            disabled={removeLine.isPending}
                            className="rounded-lg p-1.5 text-red-500 transition-colors hover:bg-red-50 hover:text-red-700 disabled:opacity-50"
                            title="حذف"
                          >
                            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
                              <path
                                strokeLinecap="round"
                                strokeLinejoin="round"
                                d="m14.74 9-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 0 1-2.244 2.077H8.084a2.25 2.25 0 0 1-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 0 0-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 0 1 3.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 0 0-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 0 0-7.5 0"
                              />
                            </svg>
                          </button>
                        </div>
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
              {/* Summary Footer */}
              <tfoot className="border-t-2 border-slate-200 bg-slate-50">
                <tr>
                  <td colSpan={3} className="px-4 py-3 text-left font-semibold text-slate-700">
                    جمع کل
                  </td>
                  <td className="px-4 py-3 font-bold text-slate-900">
                    {receipt.lines.reduce((sum, l) => sum + l.qty, 0).toLocaleString('fa-IR')}
                  </td>
                  {(isReceived || isApproved) && (
                    <>
                      <td className="px-4 py-3 font-bold text-green-600">
                        {receipt.lines.reduce((sum, l) => sum + l.approvedQty, 0).toLocaleString('fa-IR')}
                      </td>
                      <td className="px-4 py-3 font-bold text-red-600">
                        {receipt.lines.reduce((sum, l) => sum + l.rejectedQty, 0).toLocaleString('fa-IR')}
                      </td>
                      <td className="px-4 py-3 font-bold text-blue-600">
                        {receipt.lines.reduce((sum, l) => sum + l.remainingQty, 0).toLocaleString('fa-IR')}
                      </td>
                      {isReceived && <td></td>}
                    </>
                  )}
                  {isDraft && <td colSpan={4}></td>}
                </tr>
              </tfoot>
            </table>
          </div>
        )}
      </div>

      {/* Add Line Modal */}
      <AddReceiptLineModal
        isOpen={showAddLineModal}
        onClose={() => setShowAddLineModal(false)}
        onSubmit={handleAddLine}
        isSubmitting={addLine.isPending}
      />

      {/* Edit Line Modal */}
      <EditReceiptLineModal
        isOpen={!!editingLine}
        line={editingLine}
        onClose={() => setEditingLine(null)}
        onSubmit={handleUpdateLine}
        isSubmitting={updateLine.isPending}
      />

      {/* Approve/Reject Line Modal */}
      <ApproveRejectLineModal
        isOpen={!!approveRejectLine}
        line={approveRejectLine?.line || null}
        mode={approveRejectLine?.mode || 'approve'}
        onClose={() => setApproveRejectLine(null)}
        onSubmit={handleApproveRejectLine}
        isSubmitting={approveLinePartial.isPending || rejectLine.isPending}
      />
    </div>
  )
}
