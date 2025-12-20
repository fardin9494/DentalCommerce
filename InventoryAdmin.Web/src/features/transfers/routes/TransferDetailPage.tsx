import { useState, useMemo } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { PageHeader } from '@/shared/components/PageHeader'
import { Spinner } from '@/shared/components/Spinner'
import {
  useTransfer,
  useAddTransferLine,
  useRemoveTransferLine,
  useUpdateTransferHeader,
  useUpdateTransferLine,
  useAllocateTransferLineFefo,
  useAllocateTransferLineFifo,
  useAllocateTransferLineLifo,
  useShipTransfer,
  useReceiveTransfer,
  useCompleteTransfer,
  useCancelTransfer,
} from '../queries'
import { useConfirm } from '@/shared/components/confirm/ConfirmProvider'
import { AddTransferLineModal } from '../components/AddTransferLineModal'
import { EditTransferLineModal } from '../components/EditTransferLineModal'
import { AllocateMethodModal } from '../components/AllocateMethodModal'
import { ReceiveSegmentModal } from '../components/ReceiveSegmentModal'
import type { TransferLine, TransferSegment } from '../types'
import { TransferStatusLabels, TransferStatusColors, type TransferStatus } from '../types'
import { useActiveWarehouses } from '@/shared/hooks/useWarehouses'
import { useProductNames } from '@/shared/hooks/useProductNames'
import { transferDisplayRef } from '@/shared/utils/inventoryDocumentReference'

export function TransferDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { data: transfer, isLoading, error } = useTransfer(id)
  const { data: warehouses } = useActiveWarehouses()
  const addLine = useAddTransferLine(id!)
  const removeLine = useRemoveTransferLine(id!)
  const updateHeader = useUpdateTransferHeader(id!)
  const updateLine = useUpdateTransferLine(id!)
  const allocateFefo = useAllocateTransferLineFefo(id!)
  const allocateFifo = useAllocateTransferLineFifo(id!)
  const allocateLifo = useAllocateTransferLineLifo(id!)
  const ship = useShipTransfer(id!)
  const receive = useReceiveTransfer(id!)
  const complete = useCompleteTransfer(id!)
  const cancel = useCancelTransfer(id!)
  const confirm = useConfirm()
  const [showAddLineModal, setShowAddLineModal] = useState(false)
  const [editingLine, setEditingLine] = useState<TransferLine | null>(null)
  const [receivingSegment, setReceivingSegment] = useState<{ segment: TransferSegment; line: TransferLine } | null>(
    null
  )
  const [allocatingLineId, setAllocatingLineId] = useState<string | null>(null)

  // جمع‌آوری productId های تمام خطوط برای fetch کردن نام محصولات
  const productIds = useMemo(() => transfer?.lines.map((line) => line.productId) || [], [transfer?.lines])
  const { getProductName, getVariantName } = useProductNames(productIds)

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-20">
        <Spinner className="h-8 w-8 text-emerald-600" />
      </div>
    )
  }

  if (error || !transfer) {
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
        <p className="text-lg font-medium text-red-800">انتقال پیدا نشد</p>
        <p className="mt-2 text-sm text-red-600">{(error as Error)?.message || 'خطا در دریافت اطلاعات انتقال'}</p>
        <button
          onClick={() => navigate('/transfers')}
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

  async function handleAddLine(data: { productId: string; variantId?: string; qty: number }) {
    await addLine.mutateAsync({
      productId: data.productId,
      variantId: data.variantId || null,
      qty: data.qty,
    })
    setShowAddLineModal(false)
  }

  async function handleRemoveLine(lineId: string) {
    const ok = await confirm.confirm({ title: 'حذف خط', message: 'آیا از حذف این خط مطمئن هستید؟' })
    if (!ok) return
    await removeLine.mutateAsync(lineId)
  }

  async function handleUpdateLine(data: { qty?: number }) {
    if (!editingLine) return
    await updateLine.mutateAsync({
      lineId: editingLine.id,
      dto: data,
    })
    setEditingLine(null)
  }

  async function handleUpdateHeader() {
    const externalRef = await swalPrompt({
      title: 'ویرایش هدر انتقال',
      inputLabel: 'شماره مرجع خارجی',
      defaultValue: transfer?.externalRef || '',
      required: false,
    })
    const docDate = await swalPrompt({
      title: 'تاریخ سند',
      inputLabel: 'تاریخ سند (ISO)',
      defaultValue: transfer?.docDate ? new Date(transfer.docDate).toISOString().split('T')[0] : '',
      required: false,
    })
    await updateHeader.mutateAsync({
      externalRef: externalRef || null,
      docDateUtc: docDate ? new Date(docDate).toISOString() : null,
    })
  }

  async function handleAllocateFefo(lineId: string) {
    const ok = await confirm.confirm({
      title: 'تخصیص موجودی (FEFO)',
      message: 'آیا می‌خواهید موجودی این خط را به صورت خودکار با روش FEFO (اول انقضا، اول خروج) تخصیص دهید؟',
    })
    if (!ok) return
    await allocateFefo.mutateAsync(lineId)
  }

  async function handleAllocateFifo(lineId: string) {
    const ok = await confirm.confirm({
      title: 'تخصیص موجودی (FIFO)',
      message: 'آیا می‌خواهید موجودی این خط را به صورت خودکار با روش FIFO (اول ورود، اول خروج) تخصیص دهید؟',
    })
    if (!ok) return
    await allocateFifo.mutateAsync(lineId)
  }

  async function handleAllocateLifo(lineId: string) {
    const ok = await confirm.confirm({
      title: 'تخصیص موجودی (LIFO)',
      message: 'آیا می‌خواهید موجودی این خط را به صورت خودکار با روش LIFO (آخر ورود، اول خروج) تخصیص دهید؟',
    })
    if (!ok) return
    await allocateLifo.mutateAsync(lineId)
  }

  async function handleShip() {
    const ok = await confirm.confirm({
      title: 'ارسال انتقال',
      message: 'آیا می‌خواهید این انتقال را ارسال کنید؟ موجودی از انبار مبدا کسر خواهد شد.',
    })
    if (!ok) return
    await ship.mutateAsync()
  }

  async function handleReceiveSegment(qty: number) {
    if (!receivingSegment) return
    await receive.mutateAsync({
      segmentId: receivingSegment.segment.id,
      qty,
    })
  }

  async function handleComplete() {
    const ok = await confirm.confirm({
      title: 'تایید نهایی انتقال',
      message: 'آیا از تایید نهایی این انتقال مطمئن هستید؟ پس از تایید، وضعیت انتقال به "تکمیل شده" تغییر خواهد کرد.',
    })
    if (!ok) return
    await complete.mutateAsync()
  }

  async function handleCancel() {
    const ok = await confirm.confirm({
      title: 'لغو انتقال',
      message: 'آیا می‌خواهید این انتقال را لغو کنید؟ این عملیات قابل بازگشت نیست.',
    })
    if (!ok) return
    await cancel.mutateAsync()
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

  const isDraft = transfer.status === 'Draft'
  const isShipped = transfer.status === 'Shipped' || transfer.status === 'PartiallyReceived'
  const isCompleted = transfer.status === 'Completed'
  const isPartiallyReceived = transfer.status === 'PartiallyReceived'
  
  // بررسی اینکه آیا تمام segments دریافت شده‌اند
  const allSegmentsReceived = transfer.lines.every(
    (line) => line.segments.every((seg) => seg.remainingToReceive <= 0)
  )
  const canShip = isDraft && transfer.lines.length > 0 && transfer.lines.every((l) => l.remainingQty === 0)
  
  // بررسی اینکه آیا segments دریافت نشده وجود دارد
  const hasUnreceivedSegments = transfer.lines.some(
    (line) => line.segments.some((seg) => seg.remainingToReceive > 0)
  )

  const sourceWarehouseName = warehouses?.find((w) => w.id === transfer.sourceWarehouseId)?.name || '-'
  const destinationWarehouseName = warehouses?.find((w) => w.id === transfer.destinationWarehouseId)?.name || '-'

  return (
    <div className="space-y-6">
      {/* Header */}
      <PageHeader
        title={`انتقال #${transfer.id.substring(0, 8)}`}
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
                  onClick={handleShip}
                  disabled={ship.isPending || !canShip}
                  className="inline-flex items-center gap-2 rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white shadow-sm transition-colors hover:bg-blue-700 disabled:opacity-50"
                >
                  <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 16.875h3.375m0 0h3.375m-3.375 0V13.5m0 3.375v3.375M6 10.5h2.25a2.25 2.25 0 0 0 2.25-2.25V6a2.25 2.25 0 0 0-2.25-2.25H6A2.25 2.25 0 0 0 3.75 6v2.25A2.25 2.25 0 0 0 6 10.5Zm0 9.75h2.25A2.25 2.25 0 0 0 10.5 18v-2.25a2.25 2.25 0 0 0-2.25-2.25H6a2.25 2.25 0 0 0-2.25 2.25V18A2.25 2.25 0 0 0 6 19.5Zm9.75-9.75H18a2.25 2.25 0 0 0 2.25-2.25V6A2.25 2.25 0 0 0 18 3.75h-2.25A2.25 2.25 0 0 0 13.5 6v2.25a2.25 2.25 0 0 0 2.25 2.25Z" />
                  </svg>
                  ارسال
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
            {isShipped && hasUnreceivedSegments && (
              <div className="rounded-lg border border-blue-200 bg-blue-50 px-4 py-2 text-sm font-medium text-blue-800">
                <svg
                  className="mr-2 inline h-4 w-4"
                  fill="none"
                  viewBox="0 0 24 24"
                  strokeWidth={2}
                  stroke="currentColor"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    d="M13.5 16.875h3.375m0 0h3.375m-3.375 0V13.5m0 3.375v3.375M6 10.5h2.25a2.25 2.25 0 0 0 2.25-2.25V6a2.25 2.25 0 0 0-2.25-2.25H6A2.25 2.25 0 0 0 3.75 6v2.25A2.25 2.25 0 0 0 6 10.5Zm0 9.75h2.25A2.25 2.25 0 0 0 10.5 18v-2.25a2.25 2.25 0 0 0-2.25-2.25H6a2.25 2.25 0 0 0-2.25 2.25V18A2.25 2.25 0 0 0 6 19.5Zm9.75-9.75H18a2.25 2.25 0 0 0 2.25-2.25V6A2.25 2.25 0 0 0 18 3.75h-2.25A2.25 2.25 0 0 0 13.5 6v2.25a2.25 2.25 0 0 0 2.25 2.25Z"
                  />
                </svg>
                برای دریافت کالاها، از دکمه‌های "دریافت" در ستون "دریافت" جدول خطوط انتقال استفاده کنید
              </div>
            )}
            {isPartiallyReceived && allSegmentsReceived && (
              <button
                onClick={handleComplete}
                disabled={complete.isPending}
                className="inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-4 py-2 text-sm font-medium text-white shadow-sm transition-colors hover:bg-emerald-700 disabled:opacity-50"
              >
                <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    d="M9 12.75 11.25 15 15 9.75M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"
                  />
                </svg>
                {complete.isPending ? 'در حال تایید...' : 'تایید نهایی انتقال'}
              </button>
            )}
            {isCompleted && (
              <div className="rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-2 text-sm font-medium text-emerald-800">
                <svg
                  className="mr-2 inline h-4 w-4"
                  fill="none"
                  viewBox="0 0 24 24"
                  strokeWidth={2}
                  stroke="currentColor"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    d="M9 12.75 11.25 15 15 9.75M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"
                  />
                </svg>
                انتقال تکمیل شد
              </div>
            )}
            <button
              onClick={() => navigate('/transfers')}
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
        مدیریت انتقال بین انبارها
      </PageHeader>

      {/* Info Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        {/* Status Card */}
        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
          <div className="text-sm font-medium text-slate-500">وضعیت</div>
          <div className="mt-2">
            <span
              className={`inline-flex items-center rounded-full px-3 py-1 text-sm font-medium ${
                TransferStatusColors[transfer.status as TransferStatus] || 'bg-slate-100 text-slate-700'
              }`}
            >
              {TransferStatusLabels[transfer.status as TransferStatus] || transfer.status}
            </span>
          </div>
        </div>

        {/* Source Warehouse Card */}
        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
          <div className="text-sm font-medium text-slate-500">انبار مبدا</div>
          <div className="mt-2 text-lg font-semibold text-slate-900">{sourceWarehouseName}</div>
        </div>

        {/* Destination Warehouse Card */}
        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
          <div className="text-sm font-medium text-slate-500">انبار مقصد</div>
          <div className="mt-2 text-lg font-semibold text-slate-900">{destinationWarehouseName}</div>
        </div>

        {/* Doc Date Card */}
        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
          <div className="text-sm font-medium text-slate-500">تاریخ سند</div>
          <div className="mt-2 text-lg font-semibold text-slate-900">{formatDate(transfer.docDate)}</div>
        </div>
      </div>

      {/* Details Card */}
      <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
        <h3 className="mb-4 text-lg font-semibold text-slate-900">اطلاعات تکمیلی</h3>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <div>
            <div className="text-sm text-slate-500">شناسه انتقال</div>
            <div className="mt-1 font-mono text-sm text-slate-700">{transfer.id}</div>
          </div>
          <div>
            <div className="text-sm text-slate-500">مرجع خارجی</div>
            <div className="mt-1 text-sm text-slate-700">{transferDisplayRef(transfer.id, transfer.externalRef)}</div>
          </div>
          <div>
            <div className="text-sm text-slate-500">تاریخ ارسال</div>
            <div className="mt-1 text-sm text-slate-700">{formatDateTime(transfer.shippedAt)}</div>
          </div>
          <div>
            <div className="text-sm text-slate-500">تاریخ تکمیل</div>
            <div className="mt-1 text-sm text-slate-700">{formatDateTime(transfer.completedAt)}</div>
          </div>
        </div>
      </div>

      {/* Lines Table */}
      <div className="rounded-xl border border-slate-200 bg-white shadow-sm overflow-hidden">
        <div className="flex items-center justify-between border-b border-slate-200 bg-slate-50 px-6 py-4">
          <h3 className="text-lg font-semibold text-slate-900">
            خطوط انتقال
            <span className="mr-2 rounded-full bg-slate-200 px-2.5 py-0.5 text-sm font-medium text-slate-600">
              {transfer.lines.length}
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

        {transfer.lines.length === 0 ? (
          <div className="py-12 text-center">
            <div className="mx-auto mb-4 h-12 w-12 rounded-full bg-slate-100 p-3 text-slate-400">
              <svg fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  d="M7.5 21 3 16.5m0 0L7.5 12M3 16.5h13.5m0-13.5L21 7.5m0 0L16.5 12M21 7.5H7.5"
                />
              </svg>
            </div>
            <p className="text-slate-600">هیچ خطی اضافه نشده است</p>
            {isDraft && <p className="mt-1 text-sm text-slate-400">برای شروع یک خط جدید اضافه کنید</p>}
          </div>
        ) : (
          <div className="overflow-x-auto relative">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-slate-200 bg-slate-50 text-right">
                  <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">ردیف</th>
                  <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">شناسه محصول</th>
                  <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">واریانت</th>
                  <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600 text-center">مقدار درخواستی</th>
                  <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600 text-center">تخصیص یافته</th>
                  <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600 text-center">باقی‌مانده</th>
                  <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">بخش‌ها</th>
                  {isDraft && <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">عملیات</th>}
                  {isShipped && <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">دریافت</th>}
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {transfer.lines.map((line) => (
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
                    <td className="whitespace-nowrap px-4 py-3 text-center font-medium text-slate-900">
                      {line.requestedQty.toLocaleString('fa-IR')}
                    </td>
                    <td className="whitespace-nowrap px-4 py-3 text-center text-slate-600">
                      {line.allocatedQty.toLocaleString('fa-IR')}
                    </td>
                    <td className="whitespace-nowrap px-4 py-3 text-center">
                      <span
                        className={`font-medium ${
                          line.remainingQty > 0 ? 'text-orange-600' : 'text-emerald-600'
                        }`}
                      >
                        {line.remainingQty.toLocaleString('fa-IR')}
                      </span>
                    </td>
                    <td className="px-4 py-3">
                      {line.segments.length === 0 ? (
                        <span className="text-xs text-slate-400">بدون تخصیص</span>
                      ) : (
                        <div className="space-y-2">
                          {line.segments.map((seg) => (
                            <div key={seg.id} className="rounded-lg border border-slate-200 bg-slate-50 p-2 text-xs">
                              <div className="flex items-start justify-between gap-2">
                                <div className="flex-1 min-w-0">
                                  {seg.sku && (
                                    <div className="font-medium text-slate-900">
                                      SKU: <span className="font-mono">{seg.sku}</span>
                                    </div>
                                  )}
                                  {seg.lotNumber && (
                                    <div className="mt-0.5 text-slate-600">لات: {seg.lotNumber}</div>
                                  )}
                                  {seg.expiryDate && (
                                    <div className="mt-0.5 text-slate-600">
                                      انقضا: {formatDate(seg.expiryDate)}
                                    </div>
                                  )}
                                  {seg.shelfName && (
                                    <div className="mt-0.5">
                                      <span className="inline-flex items-center gap-1 rounded-full bg-emerald-100 px-2 py-0.5 text-xs font-medium text-emerald-700">
                                        <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                                          <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 16.875h3.375m0 0h3.375m-3.375 0V13.5m0 3.375v3.375M6 10.5h2.25a2.25 2.25 0 0 0 2.25-2.25V6a2.25 2.25 0 0 0-2.25-2.25H6A2.25 2.25 0 0 0 3.75 6v2.25A2.25 2.25 0 0 0 6 10.5Zm0 9.75h2.25A2.25 2.25 0 0 0 10.5 18v-2.25a2.25 2.25 0 0 0-2.25-2.25H6a2.25 2.25 0 0 0-2.25 2.25V18A2.25 2.25 0 0 0 6 19.5Zm9.75-9.75H18a2.25 2.25 0 0 0 2.25-2.25V6A2.25 2.25 0 0 0 18 3.75h-2.25A2.25 2.25 0 0 0 13.5 6v2.25a2.25 2.25 0 0 0 2.25 2.25Z" />
                                        </svg>
                                        {seg.shelfName}
                                      </span>
                                    </div>
                                  )}
                                </div>
                                <div className="text-left">
                                  <div className="font-semibold text-slate-900">
                                    {seg.qty.toLocaleString('fa-IR')}
                                  </div>
                                  {seg.receivedQty > 0 ? (
                                    <div className="mt-0.5 text-xs text-emerald-600">
                                      دریافت: {seg.receivedQty.toLocaleString('fa-IR')}
                                    </div>
                                  ) : (
                                    <div className="mt-0.5 text-xs text-orange-600">در انتظار</div>
                                  )}
                                </div>
                              </div>
                            </div>
                          ))}
                        </div>
                      )}
                    </td>
                    {isDraft && (
                      <td className="whitespace-nowrap px-4 py-3 relative">
                        <div className="flex items-center gap-1">
                          {line.remainingQty > 0 && (
                            <button
                              onClick={() => setAllocatingLineId(line.id)}
                              disabled={allocateFefo.isPending || allocateFifo.isPending || allocateLifo.isPending}
                              className="rounded-lg p-1.5 text-blue-500 transition-colors hover:bg-blue-50 hover:text-blue-700 disabled:opacity-50"
                              title="تخصیص موجودی"
                            >
                              <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
                                <path
                                  strokeLinecap="round"
                                  strokeLinejoin="round"
                                  d="M9 12.75 11.25 15 15 9.75m-3-7.036A11.959 11.959 0 0 1 3.598 6 11.99 11.99 0 0 0 3 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285Z"
                                />
                              </svg>
                            </button>
                          )}
                          <button
                            onClick={() => setEditingLine(line)}
                            className="rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-600"
                            title="ویرایش"
                          >
                            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
                              <path
                                strokeLinecap="round"
                                strokeLinejoin="round"
                                d="m16.862 4.487 1.687-1.688a1.875 1.875 0 1 1 2.652 2.652L10.582 16.07a4.5 4.5 0 0 1-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 0 1 1.13-1.897l8.932-8.931Z"
                              />
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
                    {isShipped && (
                      <td className="whitespace-nowrap px-4 py-3">
                        {line.segments
                          .filter((seg) => seg.remainingToReceive > 0)
                          .map((seg) => (
                            <button
                              key={seg.id}
                              onClick={() => setReceivingSegment({ segment: seg, line })}
                              className="mb-1 mr-1 rounded-lg bg-emerald-600 px-2 py-1 text-xs font-medium text-white transition-colors hover:bg-emerald-700"
                            >
                              دریافت ({seg.remainingToReceive.toLocaleString('fa-IR')})
                            </button>
                          ))}
                        {line.segments.filter((seg) => seg.remainingToReceive > 0).length === 0 && (
                          <span className="text-xs text-slate-400">تمام شده</span>
                        )}
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
                  <td className="px-4 py-3 text-center font-bold text-emerald-600">
                    {transfer.lines.reduce((sum, l) => sum + l.requestedQty, 0).toLocaleString('fa-IR')}
                  </td>
                  <td className="px-4 py-3 text-center font-semibold text-slate-700">
                    {transfer.lines.reduce((sum, l) => sum + l.allocatedQty, 0).toLocaleString('fa-IR')}
                  </td>
                  <td className="px-4 py-3 text-center font-semibold text-slate-700">
                    {transfer.lines.reduce((sum, l) => sum + l.remainingQty, 0).toLocaleString('fa-IR')}
                  </td>
                  <td colSpan={isDraft ? 1 : isShipped ? 1 : 0}></td>
                </tr>
              </tfoot>
            </table>
          </div>
        )}

        {/* بخش بارهای دریافت شده */}
        {isShipped && (
          <div className="rounded-xl border border-slate-200 bg-white shadow-sm">
            <div className="border-b border-slate-200 bg-slate-50 px-6 py-4">
              <h2 className="text-lg font-semibold text-slate-900">بارهای دریافت شده در انبار مقصد</h2>
              <p className="mt-1 text-sm text-slate-600">
                لیست کالاهایی که در انبار مقصد ({destinationWarehouseName}) دریافت شده‌اند
              </p>
            </div>
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead className="border-b border-slate-200 bg-slate-50">
                  <tr>
                    <th className="px-4 py-3 text-right text-sm font-semibold text-slate-700">ردیف</th>
                    <th className="px-4 py-3 text-right text-sm font-semibold text-slate-700">SKU</th>
                    <th className="px-4 py-3 text-right text-sm font-semibold text-slate-700">لات</th>
                    <th className="px-4 py-3 text-right text-sm font-semibold text-slate-700">تاریخ انقضا</th>
                    <th className="px-4 py-3 text-center text-sm font-semibold text-slate-700">مقدار ارسال شده</th>
                    <th className="px-4 py-3 text-center text-sm font-semibold text-slate-700">مقدار دریافت شده</th>
                    <th className="px-4 py-3 text-center text-sm font-semibold text-slate-700">وضعیت</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-200">
                  {transfer.lines.flatMap((line, lineIdx) =>
                    line.segments
                      .filter((seg) => seg.receivedQty > 0)
                      .map((seg, segIdx) => (
                        <tr key={seg.id} className="hover:bg-slate-50">
                          <td className="whitespace-nowrap px-4 py-3 text-sm text-slate-600">
                            {lineIdx + 1}-{segIdx + 1}
                          </td>
                          <td className="whitespace-nowrap px-4 py-3">
                            {seg.sku ? (
                              <span className="font-mono text-sm font-medium text-slate-900">{seg.sku}</span>
                            ) : (
                              <span className="text-sm text-slate-400">-</span>
                            )}
                          </td>
                          <td className="whitespace-nowrap px-4 py-3 text-sm text-slate-600">
                            {seg.lotNumber || '-'}
                          </td>
                          <td className="whitespace-nowrap px-4 py-3 text-sm text-slate-600">
                            {seg.expiryDate ? formatDate(seg.expiryDate) : '-'}
                          </td>
                          <td className="whitespace-nowrap px-4 py-3 text-center text-sm font-medium text-slate-700">
                            {seg.qty.toLocaleString('fa-IR')}
                          </td>
                          <td className="whitespace-nowrap px-4 py-3 text-center">
                            <span className="inline-flex items-center gap-1 rounded-full bg-emerald-100 px-3 py-1 text-sm font-semibold text-emerald-700">
                              <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                                <path
                                  strokeLinecap="round"
                                  strokeLinejoin="round"
                                  d="M9 12.75 11.25 15 15 9.75m-3-7.036A11.959 11.959 0 0 1 3.598 6 11.99 11.99 0 0 0 3 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285Z"
                                />
                              </svg>
                              {seg.receivedQty.toLocaleString('fa-IR')}
                            </span>
                          </td>
                          <td className="whitespace-nowrap px-4 py-3 text-center">
                            {seg.remainingToReceive <= 0 ? (
                              <span className="inline-flex items-center gap-1 rounded-full bg-emerald-100 px-3 py-1 text-xs font-medium text-emerald-700">
                                <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                                  <path
                                    strokeLinecap="round"
                                    strokeLinejoin="round"
                                    d="M9 12.75 11.25 15 15 9.75M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"
                                  />
                                </svg>
                                دریافت کامل
                              </span>
                            ) : (
                              <span className="inline-flex items-center gap-1 rounded-full bg-orange-100 px-3 py-1 text-xs font-medium text-orange-700">
                                <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                                  <path
                                    strokeLinecap="round"
                                    strokeLinejoin="round"
                                    d="M12 6v6h4.5m4.5 0a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"
                                  />
                                </svg>
                                در انتظار ({seg.remainingToReceive.toLocaleString('fa-IR')})
                              </span>
                            )}
                          </td>
                        </tr>
                      ))
                  )}
                  {transfer.lines.flatMap((line) => line.segments).filter((seg) => seg.receivedQty > 0).length === 0 && (
                    <tr>
                      <td colSpan={7} className="px-4 py-8 text-center text-sm text-slate-500">
                        هنوز هیچ باری دریافت نشده است
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
            {allSegmentsReceived && !isCompleted && (
              <div className="border-t border-slate-200 bg-emerald-50 px-6 py-4">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <svg className="h-5 w-5 text-emerald-600" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        d="M9 12.75 11.25 15 15 9.75M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"
                      />
                    </svg>
                    <span className="text-sm font-medium text-emerald-800">تمام کالاها دریافت شدند و آماده تایید نهایی هستند</span>
                  </div>
                  <button
                    onClick={handleComplete}
                    disabled={complete.isPending}
                    className="inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-4 py-2 text-sm font-medium text-white shadow-sm transition-colors hover:bg-emerald-700 disabled:opacity-50"
                  >
                    <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        d="M9 12.75 11.25 15 15 9.75M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"
                      />
                    </svg>
                    {complete.isPending ? 'در حال تایید...' : 'تایید نهایی انتقال'}
                  </button>
                </div>
              </div>
            )}
          </div>
        )}
      </div>

      {/* Add Line Modal */}
      <AddTransferLineModal
        isOpen={showAddLineModal}
        onClose={() => setShowAddLineModal(false)}
        onSubmit={handleAddLine}
        isSubmitting={addLine.isPending}
      />

      {/* Edit Line Modal */}
      <EditTransferLineModal
        isOpen={!!editingLine}
        line={editingLine}
        onClose={() => setEditingLine(null)}
        onSubmit={handleUpdateLine}
        isSubmitting={updateLine.isPending}
      />

      {/* Allocate Method Modal */}
      <AllocateMethodModal
        isOpen={!!allocatingLineId}
        onClose={() => setAllocatingLineId(null)}
        onSelectFefo={() => allocatingLineId && handleAllocateFefo(allocatingLineId)}
        onSelectFifo={() => allocatingLineId && handleAllocateFifo(allocatingLineId)}
        onSelectLifo={() => allocatingLineId && handleAllocateLifo(allocatingLineId)}
        isAllocating={allocateFefo.isPending || allocateFifo.isPending || allocateLifo.isPending}
      />

      {/* Receive Segment Modal */}
      <ReceiveSegmentModal
        isOpen={!!receivingSegment}
        segment={receivingSegment?.segment || null}
        onClose={() => setReceivingSegment(null)}
        onSubmit={handleReceiveSegment}
        isSubmitting={receive.isPending}
      />
    </div>
  )
}
