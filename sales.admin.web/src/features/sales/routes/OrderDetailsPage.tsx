import { useEffect, useState, type ReactNode } from 'react'
import { Link, useParams } from 'react-router-dom'
import { fetchJson, fetchJsonWithBase } from '@/lib/api/client'
import { CATALOG_API_BASE, INVENTORY_API_BASE } from '@/app/env'
import { useToast } from '@/shared/components/toast/ToastProvider'
import { formatDate, formatNumber } from '@/shared/utils/date'

type OrderLine = {
  skuId: string
  batchId?: string | null
  quantity: number
  cancelledQty: number
  returnedQty: number
  refundedQty: number
  baseUnitPrice: number
  finalUnitPrice: number
  isGift: boolean
}

type OrderDetail = {
  id: string
  siteId: string
  userId?: string | null
  orderNumber: string
  pricingQuoteId: string
  currency: string
  status: string
  createdAt: string
  updatedAt: string
  placedAtUtc?: string | null
  cancelledAtUtc?: string | null
  paymentFailedAtUtc?: string | null
  paymentFailureReason?: string | null
  paymentFailureDetails?: string | null
  subtotal: number
  discountTotal: number
  finalTotal: number
  cashbackTotal: number
  lines: OrderLine[]
}

type ReservationItem = {
  id: string
  orderId: string
  stockItemId: string
  skuId: string
  qty: number
  createdAt: string
}

type StoreInfo = {
  id: string
  name: string
  domain?: string | null
}

type ProductInfo = {
  productId: string
  variantId?: string | null
  name: string
  code: string
}

type StockItemInfo = {
  id: string
  productName?: string | null
  variantValue?: string | null
  sku: string
  warehouseName?: string | null
}

type TimelineItem = {
  id: string
  orderId: string
  eventType: string
  fromStatus?: string | null
  toStatus?: string | null
  message?: string | null
  dataJson?: string | null
  createdAt: string
}

type RefundLine = {
  id: string
  orderLineId: string
  skuId: string
  batchId?: string | null
  quantity: number
  unitAmount: number
  lineAmount: number
}

type RefundRequest = {
  id: string
  orderId: string
  status: string
  destination: string
  currency: string
  amount: number
  reason?: string | null
  note?: string | null
  requestedBy?: string | null
  requestedAtUtc: string
  approvedAtUtc?: string | null
  rejectedAtUtc?: string | null
  completedAtUtc?: string | null
  rejectionReason?: string | null
  walletReference?: string | null
  lines: RefundLine[]
}

type OrderNote = {
  id: string
  orderId: string
  note: string
  createdBy?: string | null
  isInternal: boolean
  createdAt: string
  updatedAt: string
}

export function OrderDetailsPage() {
  const { id } = useParams()
  const toast = useToast()
  const [order, setOrder] = useState<OrderDetail | null>(null)
  const [reservations, setReservations] = useState<ReservationItem[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [reservationError, setReservationError] = useState<string | null>(null)
  const [timeline, setTimeline] = useState<TimelineItem[]>([])
  const [timelineError, setTimelineError] = useState<string | null>(null)
  const [notes, setNotes] = useState<OrderNote[]>([])
  const [notesLoading, setNotesLoading] = useState(false)
  const [notesError, setNotesError] = useState<string | null>(null)
  const [newNote, setNewNote] = useState('')
  const [newNoteCreatedBy, setNewNoteCreatedBy] = useState('')
  const [newNoteIsInternal, setNewNoteIsInternal] = useState(false)
  const [addingNote, setAddingNote] = useState(false)
  const [actionNote, setActionNote] = useState('')
  const [trackingCode, setTrackingCode] = useState('')
  const [carrier, setCarrier] = useState('')
  const [returnReason, setReturnReason] = useState('')
  const [refundReason, setRefundReason] = useState('')
  const [refundAmount, setRefundAmount] = useState<string>('')
  const [cancelReason, setCancelReason] = useState('')
  const [storeInfo, setStoreInfo] = useState<StoreInfo | null>(null)
  const [productInfoMap, setProductInfoMap] = useState<Map<string, ProductInfo>>(new Map())
  const [stockItemInfoMap, setStockItemInfoMap] = useState<Map<string, StockItemInfo>>(new Map())
  const [refunds, setRefunds] = useState<RefundRequest[]>([])
  const [refundsLoading, setRefundsLoading] = useState(false)
  const [refundsError, setRefundsError] = useState<string | null>(null)
  const [partialLineKey, setPartialLineKey] = useState('')
  const [partialQty, setPartialQty] = useState('')
  const [partialReason, setPartialReason] = useState('')
  const [partialNote, setPartialNote] = useState('')
  const [partialRequestedBy, setPartialRequestedBy] = useState('')
  const [partialSubmitting, setPartialSubmitting] = useState(false)
  const [refundNotes, setRefundNotes] = useState<Record<string, string>>({})
  const [refundRejectReasons, setRefundRejectReasons] = useState<Record<string, string>>({})
  const [refundWalletRefs, setRefundWalletRefs] = useState<Record<string, string>>({})
  const [refundActionLoading, setRefundActionLoading] = useState<string | null>(null)

  useEffect(() => {
    if (!id) return
    let ignore = false
    async function load() {
      setLoading(true)
      setError(null)
      setReservationError(null)
      try {
        const res = await fetchJson<OrderDetail>(`/sales/orders/${id}`)
        if (!ignore) {
          setOrder(res)
          // Load store info
          if (res.siteId) {
            try {
              const store = await fetchJsonWithBase<StoreInfo>(CATALOG_API_BASE, `/stores/${res.siteId}`)
              if (!ignore) setStoreInfo(store)
            } catch {
              // ignore store load error
            }
          }
        }
      } catch (err: any) {
        if (!ignore) {
          const msg = err?.message || 'خطا در دریافت سفارش'
          setError(msg)
          toast.error(msg)
        }
      } finally {
        if (!ignore) setLoading(false)
      }
    }
    load()
    return () => { ignore = true }
  }, [id, toast])

  const loadTimeline = async () => {
    if (!id) return
    setTimelineError(null)
    try {
      const res = await fetchJson<TimelineItem[]>(`/sales/orders/${id}/timeline`)
      setTimeline(res || [])
    } catch (err: any) {
      const msg = err?.message || 'خطا در دریافت لاگ سفارش'
      setTimelineError(msg)
    }
  }

  useEffect(() => {
    if (!id) return
    loadTimeline()
  }, [id])

  const loadNotes = async () => {
    if (!id) return
    setNotesLoading(true)
    setNotesError(null)
    try {
      const res = await fetchJson<OrderNote[]>(`/sales/orders/${id}/notes?includeInternal=true`)
      setNotes(res || [])
    } catch (err: any) {
      const msg = err?.message || 'خطا در دریافت یادداشت‌ها'
      setNotesError(msg)
    } finally {
      setNotesLoading(false)
    }
  }

  useEffect(() => {
    if (!id) return
    loadNotes()
  }, [id])

  const loadRefunds = async () => {
    if (!id) return
    setRefundsLoading(true)
    setRefundsError(null)
    try {
      const res = await fetchJson<RefundRequest[]>(`/sales/orders/${id}/refunds`)
      setRefunds(res || [])
    } catch (err: any) {
      const msg = err?.message || 'خطا در دریافت بازپرداخت‌ها'
      setRefundsError(msg)
    } finally {
      setRefundsLoading(false)
    }
  }

  useEffect(() => {
    if (!id) return
    loadRefunds()
  }, [id])

  const handleAddNote = async () => {
    if (!id || !newNote.trim()) return
    setAddingNote(true)
    try {
      await fetchJson(`/sales/orders/${id}/notes`, {
        json: {
          note: newNote.trim(),
          createdBy: newNoteCreatedBy.trim() || null,
          isInternal: newNoteIsInternal,
        },
      })
      toast.success('یادداشت با موفقیت اضافه شد')
      setNewNote('')
      setNewNoteCreatedBy('')
      setNewNoteIsInternal(false)
      await loadNotes()
    } catch (err: any) {
      const msg = err?.message || 'خطا در افزودن یادداشت'
      toast.error(msg)
    } finally {
      setAddingNote(false)
    }
  }

  useEffect(() => {
    if (!id) return
    let ignore = false
    async function loadReservations() {
      try {
        const res = await fetchJsonWithBase<ReservationItem[]>(INVENTORY_API_BASE, `/reservations/${id}`)
        if (!ignore) {
          setReservations(res || [])
          // Load stock item info for each reservation
          const stockItemIds = res?.map(r => r.stockItemId).filter(Boolean) || []
          if (stockItemIds.length > 0) {
            const stockItemInfos = new Map<string, StockItemInfo>()
            await Promise.all(
              stockItemIds.map(async (stockItemId) => {
                try {
                  const stockItem = await fetchJsonWithBase<StockItemInfo>(INVENTORY_API_BASE, `/stock-items/${stockItemId}`)
                  if (stockItem && !ignore) {
                    stockItemInfos.set(stockItemId, stockItem)
                  }
                } catch {
                  // ignore individual stock item load error
                }
              })
            )
            if (!ignore) setStockItemInfoMap(stockItemInfos)
          }
        }
      } catch (err: any) {
        if (!ignore) {
          const msg = err?.message || 'خطا در دریافت رزرو موجودی'
          setReservationError(msg)
        }
      }
    }
    loadReservations()
    return () => { ignore = true }
  }, [id])

  // Load product info for order lines
  useEffect(() => {
    if (!order?.lines.length) return
    let ignore = false
    async function loadProductInfo() {
      const productMap = new Map<string, ProductInfo>()
      await Promise.all(
        order.lines.map(async (line) => {
          try {
            const skuRes = await fetchJsonWithBase<{ productId: string; variantId?: string | null }>(
              CATALOG_API_BASE,
              `/products/by-sku?sku=${encodeURIComponent(line.skuId)}`
            )
            if (skuRes) {
              try {
                const product = await fetchJsonWithBase<{ id: string; name: string; code: string }>(
                  CATALOG_API_BASE,
                  `/products/${skuRes.productId}`
                )
                if (product && !ignore) {
                  productMap.set(line.skuId, {
                    productId: product.id,
                    variantId: skuRes.variantId,
                    name: product.name,
                    code: product.code,
                  })
                }
              } catch {
                // ignore product load error
              }
            }
          } catch {
            // ignore SKU resolution error
          }
        })
      )
      if (!ignore) setProductInfoMap(productMap)
    }
    loadProductInfo()
    return () => { ignore = true }
  }, [order?.lines])

  const canShip = order?.status === 'Placed'
  const canDeliver = order?.status === 'Shipped'
  const canReturn = order?.status === 'Delivered'
  const canRefund = order?.status === 'Returned' || order?.status === 'Delivered' || order?.status === 'Cancelled'
  const canCancel = order?.status === 'Draft' || order?.status === 'Placed'
  const canEditShipFields = order?.status === 'Placed'
  const canEditReturnFields = order?.status === 'Delivered'
  const canEditRefundFields = order?.status === 'Returned' || order?.status === 'Delivered' || order?.status === 'Cancelled'
  const canEditLines = order?.status === 'Draft' || order?.status === 'Placed'

  const handleAction = async (action: 'ship' | 'deliver' | 'return' | 'refund' | 'cancel') => {
    if (!id) return
    try {
      if (action === 'ship') {
        await fetchJson(`/sales/orders/${id}/ship`, { json: { carrier, trackingCode, note: actionNote || null } })
        toast.success('سفارش ارسال شد.')
      } else if (action === 'deliver') {
        await fetchJson(`/sales/orders/${id}/deliver`, { json: { note: actionNote || null } })
        toast.success('سفارش تحویل شد.')
      } else if (action === 'return') {
        await fetchJson(`/sales/orders/${id}/return`, { json: { reason: returnReason || null, note: actionNote || null } })
        toast.success('سفارش مرجوع شد.')
      } else if (action === 'refund') {
        await fetchJson(`/sales/orders/${id}/refund`, { json: { reason: refundReason || null, amount: refundAmount ? Number(refundAmount) : null, note: actionNote || null } })
        toast.success('سفارش Refund شد.')
      } else if (action === 'cancel') {
        await fetchJson(`/sales/orders/${id}/cancel`, { json: { reason: cancelReason || null, note: actionNote || null } })
        toast.success('سفارش لغو شد.')
      }
      setActionNote('')
      setCancelReason('')
      await Promise.all([loadTimeline(), refreshOrder(id)])
    } catch (err: any) {
      const msg = err?.message || 'خطا در انجام عملیات'
      toast.error(msg)
    }
  }

  const handlePartialCancel = async () => {
    if (!id || !order) return
    if (!partialLineKey) {
      toast.error('آیتم را انتخاب کنید.')
      return
    }
    const qty = Number(partialQty)
    if (!Number.isFinite(qty) || qty <= 0) {
      toast.error('تعداد نامعتبر است.')
      return
    }

    const { skuId, batchId } = parseLineKey(partialLineKey)
    const line = order.lines.find(l => l.skuId === skuId && (l.batchId ?? null) === (batchId ?? null))
    if (!line) {
      toast.error('آیتم انتخاب‌شده یافت نشد.')
      return
    }

    const activeQty = getActiveQty(line)
    if (qty > activeQty) {
      toast.error('تعداد لغو بیشتر از تعداد موجود است.')
      return
    }

    setPartialSubmitting(true)
    try {
      await fetchJson(`/sales/orders/${id}/cancel-lines`, {
        json: {
          reason: partialReason || null,
          note: partialNote || null,
          requestedBy: partialRequestedBy || null,
          lines: [{ skuId, batchId, qty }]
        }
      })
      toast.success('لغو جزئی ثبت شد و درخواست بازپرداخت ایجاد شد.')
      setPartialLineKey('')
      setPartialQty('')
      setPartialReason('')
      setPartialNote('')
      setPartialRequestedBy('')
      await Promise.all([loadTimeline(), refreshOrder(id), loadRefunds()])
    } catch (err: any) {
      const msg = err?.message || 'خطا در ثبت لغو جزئی'
      toast.error(msg)
    } finally {
      setPartialSubmitting(false)
    }
  }

  const handleApproveRefund = async (refundId: string) => {
    setRefundActionLoading(refundId)
    try {
      await fetchJson(`/sales/refunds/${refundId}/approve`, {
        json: { note: refundNotes[refundId] || null }
      })
      toast.success('درخواست بازپرداخت تایید شد.')
      await Promise.all([loadRefunds(), loadTimeline()])
    } catch (err: any) {
      toast.error(err?.message || 'خطا در تایید بازپرداخت')
    } finally {
      setRefundActionLoading(null)
    }
  }

  const handleRejectRefund = async (refundId: string) => {
    const reason = refundRejectReasons[refundId]
    if (!reason || reason.trim().length === 0) {
      toast.error('دلیل رد را وارد کنید.')
      return
    }
    setRefundActionLoading(refundId)
    try {
      await fetchJson(`/sales/refunds/${refundId}/reject`, {
        json: { reason }
      })
      toast.success('درخواست بازپرداخت رد شد.')
      await Promise.all([loadRefunds(), loadTimeline()])
    } catch (err: any) {
      toast.error(err?.message || 'خطا در رد بازپرداخت')
    } finally {
      setRefundActionLoading(null)
    }
  }

  const handleCompleteRefund = async (refundId: string) => {
    setRefundActionLoading(refundId)
    try {
      await fetchJson(`/sales/refunds/${refundId}/complete`, {
        json: { walletReference: refundWalletRefs[refundId] || null }
      })
      toast.success('بازپرداخت تکمیل شد.')
      await Promise.all([loadRefunds(), loadTimeline(), refreshOrder(id)])
    } catch (err: any) {
      toast.error(err?.message || 'خطا در تکمیل بازپرداخت')
    } finally {
      setRefundActionLoading(null)
    }
  }

  const refreshOrder = async (orderId: string) => {
    try {
      const res = await fetchJson<OrderDetail>(`/sales/orders/${orderId}`)
      setOrder(res)
    } catch {
      // ignore
    }
  }

  if (!id) {
    return (
      <div className="card p-4 text-sm text-red-600">شناسه سفارش نامعتبر است.</div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 mb-1">جزئیات سفارش</h1>
          <p className="text-sm text-gray-600 flex items-center gap-2">
            <span className="font-medium">{order?.orderNumber || id}</span>
            {order && (
              <span className={statusBadge(order.status)}>{order.status}</span>
            )}
          </p>
        </div>
        <Link to="/sales/orders" className="btn-secondary flex items-center gap-2">
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 19l-7-7m0 0l7-7m-7 7h18" />
          </svg>
          بازگشت به لیست
        </Link>
      </div>

      {error && (
        <div className="card p-4 bg-red-50 border-red-200 text-sm text-red-700 flex items-center gap-2">
          <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          {error}
        </div>
      )}

      {loading && !order ? (
        <div className="card p-8 text-center">
          <svg className="animate-spin h-8 w-8 mx-auto text-emerald-600" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
          <p className="mt-4 text-gray-600">در حال بارگذاری...</p>
        </div>
      ) : order && (
        <>
          {/* Order Info Card */}
          <div className="card p-6 bg-gradient-to-br from-emerald-50 to-blue-50 border-emerald-200">
            <h2 className="text-lg font-semibold text-gray-900 mb-4 flex items-center gap-2">
              <svg className="w-5 h-5 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
              اطلاعات سفارش
            </h2>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              <Info 
                label="سایت" 
                value={
                  storeInfo ? (
                    <div className="flex items-center gap-2">
                      <span className="font-medium">{storeInfo.name}</span>
                      {storeInfo.domain && (
                        <span className="text-xs text-gray-500">({storeInfo.domain})</span>
                      )}
                    </div>
                  ) : (
                    <span className="text-xs text-gray-400">در حال بارگذاری...</span>
                  )
                } 
              />
              <Info label="مشتری" value={<span className="text-sm">{order.userId ? `ID: ${order.userId.slice(0, 8)}...` : 'مهمان'}</span>} />
              <Info label="ارز" value={<span className="font-medium">{order.currency}</span>} />
              <Info label="تاریخ ایجاد" value={<span className="text-sm">{formatDate(order.createdAt)}</span>} />
              <Info label="تاریخ ثبت نهایی" value={<span className="text-sm">{formatDate(order.placedAtUtc) || '—'}</span>} />
              <Info label="آخرین بروزرسانی" value={<span className="text-sm">{formatDate(order.updatedAt)}</span>} />
              {order.paymentFailedAtUtc && (
                <Info label="پرداخت ناموفق" value={<span className="text-sm text-red-600">{formatDate(order.paymentFailedAtUtc)}</span>} />
              )}
              {order.cancelledAtUtc && (
                <Info label="لغو شده" value={<span className="text-sm text-gray-600">{formatDate(order.cancelledAtUtc)}</span>} />
              )}
            </div>
          </div>

          {/* Action Card */}
          <div className="card p-6">
            <h3 className="text-lg font-semibold text-gray-900 mb-4 flex items-center gap-2">
              <svg className="w-5 h-5 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6V4m0 2a2 2 0 100 4m0-4a2 2 0 110 4m-6 8a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4m6 6v10m6-2a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4" />
              </svg>
              مدیریت وضعیت سفارش
            </h3>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
              <div>
                <label className="label">Carrier</label>
                <input
                  className="input"
                  value={carrier}
                  onChange={e => setCarrier(e.target.value)}
                  placeholder="پست، تیپاکس..."
                  disabled={!canEditShipFields}
                  readOnly={!canEditShipFields}
                />
              </div>
              <div>
                <label className="label">Tracking Code</label>
                <input
                  className="input"
                  value={trackingCode}
                  onChange={e => setTrackingCode(e.target.value)}
                  placeholder="کد رهگیری"
                  disabled={!canEditShipFields}
                  readOnly={!canEditShipFields}
                />
              </div>
              <div>
                <label className="label">یادداشت</label>
                <input
                  className="input"
                  value={actionNote}
                  onChange={e => setActionNote(e.target.value)}
                  placeholder="یادداشت اختیاری"
                  disabled={!canShip && !canDeliver && !canReturn && !canRefund && !canCancel}
                  readOnly={!canShip && !canDeliver && !canReturn && !canRefund && !canCancel}
                />
              </div>
              {canCancel && (
                <div>
                  <label className="label">علت لغو</label>
                  <input
                    className="input"
                    value={cancelReason}
                    onChange={e => setCancelReason(e.target.value)}
                    placeholder="دلیل لغو"
                  />
                </div>
              )}
              {canReturn && (
                <div>
                  <label className="label">علت مرجوعی</label>
                  <input
                    className="input"
                    value={returnReason}
                    onChange={e => setReturnReason(e.target.value)}
                    placeholder="دلیل مرجوعی"
                    disabled={!canEditReturnFields}
                    readOnly={!canEditReturnFields}
                  />
                </div>
              )}
              {canRefund && (
                <div>
                  <label className="label">علت Refund</label>
                  <input
                    className="input"
                    value={refundReason}
                    onChange={e => setRefundReason(e.target.value)}
                    placeholder="دلیل Refund"
                    disabled={!canEditRefundFields}
                    readOnly={!canEditRefundFields}
                  />
                </div>
              )}
              {canRefund && (
                <div>
                  <label className="label">مبلغ Refund (اختیاری)</label>
                  <input
                    className="input"
                    value={refundAmount}
                    onChange={e => setRefundAmount(e.target.value)}
                    placeholder="مبلغ"
                    disabled={!canEditRefundFields}
                    readOnly={!canEditRefundFields}
                  />
                </div>
              )}
            </div>

            <div className="mt-6 flex flex-wrap gap-3">
              <button 
                className="btn flex items-center gap-2" 
                disabled={!canShip} 
                onClick={() => handleAction('ship')}
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                </svg>
                ارسال
              </button>
              <button 
                className="btn flex items-center gap-2" 
                disabled={!canDeliver} 
                onClick={() => handleAction('deliver')}
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                تحویل
              </button>
              <button 
                className="btn-secondary flex items-center gap-2" 
                disabled={!canReturn} 
                onClick={() => handleAction('return')}
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                </svg>
                مرجوعی
              </button>
              <button 
                className="btn-secondary flex items-center gap-2" 
                disabled={!canRefund} 
                onClick={() => handleAction('refund')}
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                Refund
              </button>
              <button
                className="btn-secondary flex items-center gap-2"
                disabled={!canCancel}
                onClick={() => handleAction('cancel')}
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                </svg>
                لغو سفارش
              </button>
              {!canShip && !canDeliver && !canReturn && !canRefund && !canCancel && (
                <span className="text-sm text-gray-500 flex items-center gap-2">
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                  این سفارش در وضعیت قابل تغییر نیست.
                </span>
              )}
            </div>
          </div>

          {/* Financial Summary & Errors */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
            <div className="card p-6">
              <h3 className="text-lg font-semibold text-gray-900 mb-4 flex items-center gap-2">
                <svg className="w-5 h-5 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                خلاصه مالی
              </h3>
              <div className="space-y-3">
                <Row label="جمع" value={`${formatNumber(order.subtotal)} ${order.currency}`} />
                <Row label="تخفیف" value={`${formatNumber(order.discountTotal)} ${order.currency}`} />
                <div className="pt-2 border-t border-gray-200">
                  <Row label="قیمت نهایی" value={`${formatNumber(order.finalTotal)} ${order.currency}`} className="text-lg font-bold text-emerald-600" />
                </div>
                {order.cashbackTotal > 0 && (
                  <Row label="کش‌بک" value={`${formatNumber(order.cashbackTotal)} ${order.currency}`} className="text-green-600" />
                )}
              </div>
            </div>

            <div className="card p-6">
              <h3 className="text-lg font-semibold text-gray-900 mb-4 flex items-center gap-2">
                <svg className="w-5 h-5 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                وضعیت خطا / Trace
              </h3>
              {order.paymentFailureReason ? (
                <div className="space-y-3">
                  <div className="text-sm text-red-700 font-medium bg-red-50 p-3 rounded border border-red-200">
                    {order.paymentFailureReason}
                  </div>
                  {order.paymentFailureDetails ? (
                    <pre className="text-xs whitespace-pre-wrap bg-gray-50 p-3 rounded border max-h-64 overflow-auto font-mono">
                      {order.paymentFailureDetails}
                    </pre>
                  ) : (
                    <div className="text-xs text-gray-500">Trace ثبت نشده است.</div>
                  )}
                </div>
              ) : (
                <div className="text-sm text-gray-500 flex items-center gap-2">
                  <svg className="w-5 h-5 text-green-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                  خطایی ثبت نشده است.
                </div>
              )}
            </div>
          </div>

          {/* Order Lines */}
          <div className="card p-6">
            <h3 className="text-lg font-semibold text-gray-900 mb-4 flex items-center gap-2">
              <svg className="w-5 h-5 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16 11V7a4 4 0 00-8 0v4M5 9h14l1 12H4L5 9z" />
              </svg>
              آیتم‌های سفارش
            </h3>
            <div className="overflow-x-auto rounded-lg border border-gray-200">
              <table className="min-w-full text-sm">
                <thead>
                  <tr className="bg-gradient-to-r from-gray-50 to-gray-100">
                    <th className="px-4 py-3 text-right font-semibold text-gray-700">محصول</th>
                    <th className="px-4 py-3 text-right font-semibold text-gray-700">SKU</th>
                    <th className="px-4 py-3 text-center font-semibold text-gray-700">Batch</th>
                    <th className="px-4 py-3 text-center font-semibold text-gray-700">تعداد</th>
                    <th className="px-4 py-3 text-left font-semibold text-gray-700">قیمت پایه</th>
                    <th className="px-4 py-3 text-left font-semibold text-gray-700">قیمت نهایی</th>
                    <th className="px-4 py-3 text-center font-semibold text-gray-700">هدیه</th>
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {order.lines.length === 0 && (
                    <tr>
                      <td colSpan={7} className="px-4 py-8 text-center text-gray-500">
                        <div className="flex flex-col items-center gap-2">
                          <svg className="w-12 h-12 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4" />
                          </svg>
                          <span>آیتمی ثبت نشده است</span>
                        </div>
                      </td>
                    </tr>
                  )}
                  {order.lines.map((line, idx) => {
                    const productInfo = productInfoMap.get(line.skuId)
                    return (
                      <tr key={`${line.skuId}-${idx}`} className="hover:bg-emerald-50/50 transition-colors">
                        <td className="px-4 py-3">
                          {productInfo ? (
                            <div>
                              <div className="font-medium text-gray-900">{productInfo.name}</div>
                              <div className="text-xs text-gray-500">کد: {productInfo.code}</div>
                            </div>
                          ) : (
                            <span className="text-gray-400">—</span>
                          )}
                        </td>
                        <td className="px-4 py-3 text-right font-mono text-xs">{line.skuId}</td>
                        <td className="px-4 py-3 text-center text-xs">{line.batchId || '—'}</td>
                        <td className="px-4 py-3 text-center">
                          <div className="font-medium">{line.quantity}</div>
                          {line.cancelledQty > 0 && (
                            <div className="text-xs text-red-600">لغو: {line.cancelledQty}</div>
                          )}
                          {line.returnedQty > 0 && (
                            <div className="text-xs text-amber-600">مرجوعی: {line.returnedQty}</div>
                          )}
                          {line.refundedQty > 0 && (
                            <div className="text-xs text-emerald-600">Refund: {line.refundedQty}</div>
                          )}
                          {getActiveQty(line) !== line.quantity && (
                            <div className="text-[11px] text-gray-500">فعال: {getActiveQty(line)}</div>
                          )}
                        </td>
                        <td className="px-4 py-3 text-left">{formatNumber(line.baseUnitPrice)} {order.currency}</td>
                        <td className="px-4 py-3 text-left font-semibold text-emerald-600">{formatNumber(line.finalUnitPrice)} {order.currency}</td>
                        <td className="px-4 py-3 text-center">
                          {line.isGift ? (
                            <span className="badge badge-green">بله</span>
                          ) : (
                            <span className="text-gray-400">خیر</span>
                          )}
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          </div>

          {/* Partial Cancel */}
          {canEditLines && order.lines.length > 0 && (
            <div className="card p-6">
              <h3 className="text-lg font-semibold text-gray-900 mb-4 flex items-center gap-2">
                <svg className="w-5 h-5 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6V4m0 2a2 2 0 100 4m0-4a2 2 0 110 4m-6 8a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4m6 6v10m6-2a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4" />
                </svg>
                کاهش تعداد / لغو جزئی (قبل از ارسال)
              </h3>
              <p className="text-xs text-gray-500 mb-4">
                برای کاهش تعداد یا حذف بخشی از سفارش، آیتم را انتخاب کنید. با این کار، درخواست بازپرداخت به کیف پول ایجاد می‌شود.
              </p>

              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <div>
                  <label className="label">انتخاب آیتم</label>
                  <select
                    className="input"
                    value={partialLineKey}
                    onChange={(e) => setPartialLineKey(e.target.value)}
                  >
                    <option value="">انتخاب کنید</option>
                    {order.lines
                      .filter(l => getActiveQty(l) > 0)
                      .map((line) => (
                        <option key={lineKey(line)} value={lineKey(line)}>
                          {line.skuId} {line.batchId ? `(${line.batchId})` : ''} - فعال: {getActiveQty(line)}
                        </option>
                      ))}
                  </select>
                </div>
                <div>
                  <label className="label">تعداد کاهش/لغو</label>
                  <input
                    className="input"
                    type="number"
                    min={1}
                    value={partialQty}
                    onChange={(e) => setPartialQty(e.target.value)}
                    placeholder="مثلاً 1"
                  />
                </div>
                <div>
                  <label className="label">علت لغو (اختیاری)</label>
                  <input
                    className="input"
                    value={partialReason}
                    onChange={(e) => setPartialReason(e.target.value)}
                    placeholder="دلیل لغو"
                  />
                </div>
                <div>
                  <label className="label">یادداشت (اختیاری)</label>
                  <input
                    className="input"
                    value={partialNote}
                    onChange={(e) => setPartialNote(e.target.value)}
                    placeholder="یادداشت"
                  />
                </div>
                <div>
                  <label className="label">درخواست‌دهنده (اختیاری)</label>
                  <input
                    className="input"
                    value={partialRequestedBy}
                    onChange={(e) => setPartialRequestedBy(e.target.value)}
                    placeholder="نام ادمین"
                  />
                </div>
                <div className="flex items-end">
                  <button
                    className="btn w-full"
                    onClick={handlePartialCancel}
                    disabled={partialSubmitting}
                  >
                    {partialSubmitting ? 'در حال ثبت...' : 'لغو جزئی و ایجاد Refund'}
                  </button>
                </div>
              </div>
            </div>
          )}

          {/* Reservations */}
          <div className="card p-6">
            <h3 className="text-lg font-semibold text-gray-900 mb-4 flex items-center gap-2">
              <svg className="w-5 h-5 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
              </svg>
              Snapshot رزرو موجودی
            </h3>
            {reservationError && (
              <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded text-sm text-red-700">
                {reservationError}
              </div>
            )}
            <div className="overflow-x-auto rounded-lg border border-gray-200">
              <table className="min-w-full text-sm">
                <thead>
                  <tr className="bg-gradient-to-r from-gray-50 to-gray-100">
                    <th className="px-4 py-3 text-right font-semibold text-gray-700">محصول</th>
                    <th className="px-4 py-3 text-right font-semibold text-gray-700">SKU</th>
                    <th className="px-4 py-3 text-center font-semibold text-gray-700">تعداد</th>
                    <th className="px-4 py-3 text-right font-semibold text-gray-700">انبار</th>
                    <th className="px-4 py-3 text-right font-semibold text-gray-700">زمان رزرو</th>
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {(!reservations || reservations.length === 0) && (
                    <tr>
                      <td colSpan={5} className="px-4 py-8 text-center text-gray-500">
                        <div className="flex flex-col items-center gap-2">
                          <svg className="w-12 h-12 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
                          </svg>
                          <span>رزروی ثبت نشده است</span>
                        </div>
                      </td>
                    </tr>
                  )}
                  {reservations.map(r => {
                    const stockInfo = stockItemInfoMap.get(r.stockItemId)
                    return (
                      <tr key={r.id} className="hover:bg-emerald-50/50 transition-colors">
                        <td className="px-4 py-3">
                          {stockInfo?.productName ? (
                            <div>
                              <div className="font-medium text-gray-900">{stockInfo.productName}</div>
                              {stockInfo.variantValue && (
                                <div className="text-xs text-gray-500">{stockInfo.variantValue}</div>
                              )}
                            </div>
                          ) : (
                            <span className="text-gray-400">—</span>
                          )}
                        </td>
                        <td className="px-4 py-3 text-right font-mono text-xs">{r.skuId}</td>
                        <td className="px-4 py-3 text-center font-medium">{r.qty}</td>
                        <td className="px-4 py-3 text-right text-xs text-gray-600">
                          {stockInfo?.warehouseName || '—'}
                        </td>
                        <td className="px-4 py-3 text-right text-xs">{formatDate(r.createdAt)}</td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          </div>

          {/* Notes */}
          <div className="card p-6">
            <h3 className="text-lg font-semibold text-gray-900 mb-4 flex items-center gap-2">
              <svg className="w-5 h-5 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
              </svg>
              یادداشت‌ها
            </h3>

            {/* Add Note Form */}
            <div className="mb-6 p-4 bg-gray-50 rounded-lg border border-gray-200">
              <div className="space-y-3">
                <div>
                  <label className="label flex items-center gap-2">
                    <svg className="w-4 h-4 text-gray-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                    </svg>
                    یادداشت جدید
                  </label>
                  <textarea
                    className="input min-h-[100px] resize-y"
                    value={newNote}
                    onChange={e => setNewNote(e.target.value)}
                    placeholder="یادداشت خود را وارد کنید..."
                    maxLength={2000}
                  />
                  <div className="text-xs text-gray-500 mt-1 text-left">
                    {newNote.length} / 2000 کاراکتر
                  </div>
                </div>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                  <div>
                    <label className="label flex items-center gap-2">
                      <svg className="w-4 h-4 text-gray-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                      </svg>
                      نویسنده (اختیاری)
                    </label>
                    <input
                      className="input"
                      type="text"
                      value={newNoteCreatedBy}
                      onChange={e => setNewNoteCreatedBy(e.target.value)}
                      placeholder="نام کاربر یا ID"
                      maxLength={256}
                    />
                  </div>
                  <div className="flex items-end">
                    <label className="flex items-center gap-2 cursor-pointer">
                      <input
                        type="checkbox"
                        checked={newNoteIsInternal}
                        onChange={e => setNewNoteIsInternal(e.target.checked)}
                        className="w-4 h-4 text-emerald-600 border-gray-300 rounded focus:ring-emerald-500"
                      />
                      <span className="text-sm text-gray-700 flex items-center gap-1">
                        <svg className="w-4 h-4 text-gray-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                        </svg>
                        یادداشت داخلی (فقط برای ادمین)
                      </span>
                    </label>
                  </div>
                </div>
                <button
                  className="btn flex items-center gap-2 w-full md:w-auto"
                  onClick={handleAddNote}
                  disabled={!newNote.trim() || addingNote}
                >
                  {addingNote ? (
                    <>
                      <svg className="animate-spin h-4 w-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                        <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                        <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                      </svg>
                      در حال افزودن...
                    </>
                  ) : (
                    <>
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                      </svg>
                      افزودن یادداشت
                    </>
                  )}
                </button>
              </div>
            </div>

            {/* Notes List */}
            {notesError && (
              <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded text-sm text-red-700">
                {notesError}
              </div>
            )}
            {notesLoading ? (
              <div className="text-center py-8">
                <svg className="animate-spin h-8 w-8 mx-auto text-emerald-600" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                <p className="mt-4 text-gray-600">در حال بارگذاری یادداشت‌ها...</p>
              </div>
            ) : notes.length === 0 ? (
              <div className="text-center py-8 text-gray-500">
                <svg className="w-12 h-12 mx-auto text-gray-300 mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                </svg>
                <p>یادداشتی ثبت نشده است.</p>
              </div>
            ) : (
              <div className="space-y-3">
                {notes.map((note) => (
                  <div
                    key={note.id}
                    className={`p-4 rounded-lg border transition-colors ${
                      note.isInternal
                        ? 'bg-amber-50 border-amber-200'
                        : 'bg-white border-gray-200 hover:bg-gray-50'
                    }`}
                  >
                    <div className="flex items-start justify-between gap-3 mb-2">
                      <div className="flex-1">
                        <div className="flex items-center gap-2 mb-1">
                          {note.isInternal && (
                            <span className="badge badge-amber text-xs flex items-center gap-1">
                              <svg className="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                              </svg>
                              داخلی
                            </span>
                          )}
                          {note.createdBy && (
                            <span className="text-xs text-gray-600 flex items-center gap-1">
                              <svg className="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                              </svg>
                              {note.createdBy}
                            </span>
                          )}
                        </div>
                        <p className="text-sm text-gray-900 whitespace-pre-wrap">{note.note}</p>
                      </div>
                      <div className="text-xs text-gray-500 flex-shrink-0 text-left">
                        {formatDate(note.createdAt)}
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Refunds */}
          <div className="card p-6">
            <h3 className="text-lg font-semibold text-gray-900 mb-4 flex items-center gap-2">
              <svg className="w-5 h-5 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              بازپرداخت‌ها (Wallet)
            </h3>

            {refundsError && (
              <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded text-sm text-red-700">
                {refundsError}
              </div>
            )}

            {refundsLoading ? (
              <div className="text-center py-6 text-gray-500">در حال دریافت بازپرداخت‌ها...</div>
            ) : refunds.length === 0 ? (
              <div className="text-center py-6 text-gray-500">بازپرداختی ثبت نشده است.</div>
            ) : (
              <div className="space-y-4">
                {refunds.map((refund) => (
                  <div key={refund.id} className="border rounded-lg p-4 bg-gray-50">
                    <div className="flex flex-wrap items-center justify-between gap-2 mb-2">
                      <div className="flex items-center gap-2">
                        <span className="font-semibold text-gray-900">#{refund.id.slice(0, 8)}</span>
                        <span className={refundStatusBadge(refund.status)}>{refund.status}</span>
                      </div>
                      <div className="text-sm text-gray-700">
                        {formatNumber(refund.amount)} {refund.currency}
                      </div>
                    </div>
                    <div className="text-xs text-gray-600 mb-3">
                      ثبت شده در {formatDate(refund.requestedAtUtc)} {refund.requestedBy ? ` توسط ${refund.requestedBy}` : ''}
                    </div>
                    {refund.reason && (
                      <div className="text-xs text-gray-700 mb-2">علت: {refund.reason}</div>
                    )}
                    {refund.note && (
                      <div className="text-xs text-gray-700 mb-2">یادداشت: {refund.note}</div>
                    )}
                    {refund.rejectionReason && (
                      <div className="text-xs text-red-600 mb-2">علت رد: {refund.rejectionReason}</div>
                    )}

                    <div className="bg-white border rounded p-3 text-xs text-gray-700">
                      <div className="font-semibold mb-2">آیتم‌های بازپرداخت</div>
                      <div className="space-y-1">
                        {refund.lines.map((line) => (
                          <div key={line.id} className="flex flex-wrap gap-2">
                            <span className="text-gray-500">SKU:</span>
                            <span>{line.skuId}</span>
                            <span className="text-gray-500">تعداد:</span>
                            <span>{line.quantity}</span>
                            <span className="text-gray-500">مبلغ:</span>
                            <span>{formatNumber(line.lineAmount)} {refund.currency}</span>
                          </div>
                        ))}
                      </div>
                    </div>

                    {refund.status === 'Requested' && (
                      <div className="mt-4 grid grid-cols-1 md:grid-cols-3 gap-3">
                        <div>
                          <label className="label">یادداشت تایید (اختیاری)</label>
                          <input
                            className="input"
                            value={refundNotes[refund.id] || ''}
                            onChange={(e) => setRefundNotes(prev => ({ ...prev, [refund.id]: e.target.value }))}
                            placeholder="یادداشت"
                          />
                        </div>
                        <div>
                          <label className="label">دلیل رد (در صورت نیاز)</label>
                          <input
                            className="input"
                            value={refundRejectReasons[refund.id] || ''}
                            onChange={(e) => setRefundRejectReasons(prev => ({ ...prev, [refund.id]: e.target.value }))}
                            placeholder="علت رد"
                          />
                        </div>
                        <div className="flex items-end gap-2">
                          <button
                            className="btn w-full"
                            onClick={() => handleApproveRefund(refund.id)}
                            disabled={refundActionLoading === refund.id}
                          >
                            تایید
                          </button>
                          <button
                            className="btn-secondary w-full"
                            onClick={() => handleRejectRefund(refund.id)}
                            disabled={refundActionLoading === refund.id}
                          >
                            رد
                          </button>
                        </div>
                      </div>
                    )}

                    {refund.status === 'Approved' && (
                      <div className="mt-4 grid grid-cols-1 md:grid-cols-3 gap-3">
                        <div>
                          <label className="label">ارجاع کیف پول (اختیاری)</label>
                          <input
                            className="input"
                            value={refundWalletRefs[refund.id] || ''}
                            onChange={(e) => setRefundWalletRefs(prev => ({ ...prev, [refund.id]: e.target.value }))}
                            placeholder="Wallet Reference"
                          />
                        </div>
                        <div className="flex items-end">
                          <button
                            className="btn w-full"
                            onClick={() => handleCompleteRefund(refund.id)}
                            disabled={refundActionLoading === refund.id}
                          >
                            تکمیل بازپرداخت
                          </button>
                        </div>
                      </div>
                    )}
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Timeline */}
          <div className="card p-6">
            <h3 className="text-lg font-semibold text-gray-900 mb-1 flex items-center gap-2">
              <svg className="w-5 h-5 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              خط زمانی سفارش
            </h3>
            <p className="text-xs text-gray-500 mb-4">
              در این بخش تمام اتفاقات مهم سفارش، تغییر وضعیت‌ها و توضیحات مرتبط ثبت می‌شود تا روند سفارش به‌صورت کامل قابل پیگیری باشد.
            </p>
            {timelineError && (
              <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded text-sm text-red-700">
                {timelineError}
              </div>
            )}
            <div className="space-y-4">
              {timeline.length === 0 && (
                <div className="text-center py-8 text-gray-500">
                  <svg className="w-12 h-12 mx-auto text-gray-300 mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                  <p>لاگی ثبت نشده است.</p>
                </div>
              )}
              {timeline.map((item, idx) => {
                const meta = getTimelineMeta(item)
                const details = getTimelineDetails(item)
                return (
                <div key={item.id} className="relative pl-8 border-r-2 border-gray-200 last:border-0 pb-4 last:pb-0">
                  <div className="absolute -right-2 top-0 w-4 h-4 bg-emerald-500 rounded-full border-2 border-white"></div>
                  <div className="bg-gray-50 rounded-lg p-4 hover:bg-gray-100 transition-colors">
                    <div className="flex flex-wrap items-center justify-between gap-2 mb-2">
                      <span className="font-semibold text-gray-900">{meta.title}</span>
                      <span className="text-xs text-gray-500">{formatDate(item.createdAt)}</span>
                    </div>
                    {meta.description && (
                      <div className="text-xs text-gray-600 mb-3">
                        {meta.description}
                      </div>
                    )}
                    {(item.fromStatus || item.toStatus) && (
                      <div className="text-xs text-gray-600 mb-2 flex items-center gap-2">
                        {item.fromStatus && (
                          <span className="badge badge-gray">{statusLabel(item.fromStatus)}</span>
                        )}
                        {item.fromStatus && item.toStatus && (
                          <svg className="w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                          </svg>
                        )}
                        {item.toStatus && (
                          <span className="badge badge-green">{statusLabel(item.toStatus)}</span>
                        )}
                      </div>
                    )}
                    {details.length > 0 && (
                      <div className="mt-2 bg-white border rounded p-3 text-xs text-gray-700 space-y-2">
                        {details.map((d, index) => (
                          <div key={`${item.id}-detail-${index}`} className="flex flex-wrap gap-2">
                            <span className="text-gray-500">{d.label}:</span>
                            <span className="text-gray-900">{d.value}</span>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                </div>
              )})}
            </div>
          </div>
        </>
      )}
    </div>
  )
}

function Info({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div>
      <div className="text-xs text-gray-500 mb-1">{label}</div>
      <div className="text-sm">{value}</div>
    </div>
  )
}

function Row({ label, value, className = '' }: { label: string; value: string; className?: string }) {
  return (
    <div className={`flex items-center justify-between ${className}`}>
      <span className="text-gray-600">{label}</span>
      <span className={`font-medium ${className}`}>{value}</span>
    </div>
  )
}

function statusBadge(status: string) {
  const s = status?.toLowerCase()
  if (s === 'placed') return 'badge badge-green'
  if (s === 'shipped') return 'badge badge-blue'
  if (s === 'delivered') return 'badge badge-green'
  if (s === 'returned') return 'badge badge-amber'
  if (s === 'refunded') return 'badge badge-amber'
  if (s === 'paymentfailed') return 'badge badge-red'
  if (s === 'cancelled') return 'badge badge-gray'
  return 'badge badge-gray'
}

function refundStatusBadge(status: string) {
  const s = status?.toLowerCase()
  if (s === 'requested') return 'badge badge-amber'
  if (s === 'approved') return 'badge badge-blue'
  if (s === 'completed') return 'badge badge-green'
  if (s === 'rejected') return 'badge badge-red'
  return 'badge badge-gray'
}

function statusLabel(status?: string | null) {
  const s = status?.toLowerCase()
  if (s === 'draft') return 'پیش‌نویس'
  if (s === 'placed') return 'ثبت نهایی'
  if (s === 'paymentfailed') return 'پرداخت ناموفق'
  if (s === 'cancelled') return 'لغو شده'
  if (s === 'shipped') return 'ارسال شده'
  if (s === 'delivered') return 'تحویل شده'
  if (s === 'returned') return 'مرجوعی'
  if (s === 'refunded') return 'بازپرداخت'
  return status || 'نامشخص'
}

function lineKey(line: { skuId: string; batchId?: string | null }) {
  return `${line.skuId}__${line.batchId ?? ''}`
}

function parseLineKey(key: string) {
  const [skuId, batchId] = key.split('__')
  return { skuId, batchId: batchId ? batchId : null }
}

function getActiveQty(line: OrderLine) {
  return Math.max(0, line.quantity - line.cancelledQty - line.returnedQty)
}

function parseTimelineData(dataJson?: string | null) {
  if (!dataJson) return null
  try {
    return JSON.parse(dataJson) as Record<string, any>
  } catch {
    return null
  }
}

function getTimelineMeta(item: TimelineItem) {
  const type = item.eventType?.toLowerCase()
  const data = parseTimelineData(item.dataJson)

  switch (type) {
    case 'created':
      return {
        title: 'ایجاد سفارش',
        description: 'سفارش به‌صورت پیش‌نویس ایجاد شد و هنوز ثبت نهایی نشده است.'
      }
    case 'placed':
      return {
        title: 'ثبت نهایی سفارش',
        description: 'سفارش ثبت نهایی شد و آماده پردازش است.'
      }
    case 'paymentfailed':
      return {
        title: 'پرداخت ناموفق',
        description: item.message ? `علت ناموفق بودن پرداخت: ${item.message}` : 'پرداخت با مشکل مواجه شد.'
      }
    case 'cancelled':
      return {
        title: 'لغو سفارش',
        description: item.message ? `علت لغو سفارش: ${item.message}` : 'سفارش لغو شد.'
      }
    case 'shipped': {
      const carrier = data?.carrier
      const tracking = data?.trackingCode
      const extra = carrier || tracking ? ` (حمل با ${carrier || 'نامشخص'}، کد رهگیری: ${tracking || '—'})` : ''
      return {
        title: 'ارسال سفارش',
        description: `سفارش از انبار ارسال شد${extra}.`
      }
    }
    case 'delivered':
      return {
        title: 'تحویل سفارش',
        description: 'سفارش به مشتری تحویل داده شد.'
      }
    case 'returned': {
      const reason = data?.reason
      return {
        title: 'مرجوعی سفارش',
        description: reason ? `مرجوعی ثبت شد. علت: ${reason}` : 'مرجوعی سفارش ثبت شد.'
      }
    }
    case 'linecancelled':
      return {
        title: 'لغو آیتم از سفارش',
        description: 'بخشی از سفارش لغو شد و درخواست بازپرداخت ثبت گردید.'
      }
    case 'refundrequested':
      return {
        title: 'درخواست بازپرداخت',
        description: 'درخواست بازپرداخت برای بررسی ثبت شد.'
      }
    case 'refundapproved':
      return {
        title: 'تایید بازپرداخت',
        description: 'درخواست بازپرداخت تایید شد و در انتظار واریز به کیف پول است.'
      }
    case 'refundrejected':
      return {
        title: 'رد بازپرداخت',
        description: 'درخواست بازپرداخت رد شد.'
      }
    case 'refundcompleted':
      return {
        title: 'بازپرداخت تکمیل شد',
        description: 'مبلغ بازپرداخت به کیف پول (یا سیستم مالی) اعمال شد.'
      }
    case 'reservationupdatefailed':
      return {
        title: 'خطا در بروزرسانی رزرو موجودی',
        description: 'در بروزرسانی رزرو موجودی خطایی رخ داده است.'
      }
    case 'refunded': {
      const reason = data?.reason
      const amount = data?.amount
      const amountText = typeof amount === 'number' ? `مبلغ بازپرداخت: ${formatNumber(amount)}` : null
      return {
        title: 'بازپرداخت (Refund)',
        description: reason || amountText ? `بازپرداخت ثبت شد${reason ? `، علت: ${reason}` : ''}${amountText ? `، ${amountText}` : ''}.` : 'بازپرداخت ثبت شد.'
      }
    }
    case 'noteadded':
      return {
        title: 'یادداشت جدید',
        description: 'یادداشت جدید برای این سفارش ثبت شد.'
      }
    default:
      return {
        title: item.eventType || 'رویداد',
        description: 'یک رویداد جدید برای این سفارش ثبت شد.'
      }
  }
}

function getTimelineDetails(item: TimelineItem) {
  const details: { label: string; value: string }[] = []
  const data = parseTimelineData(item.dataJson)
  const type = item.eventType?.toLowerCase()

  if (type === 'shipped') {
    if (data?.carrier) details.push({ label: 'حامل (Carrier)', value: data.carrier })
    if (data?.trackingCode) details.push({ label: 'کد رهگیری', value: data.trackingCode })
  }

  if (type === 'returned' && data?.reason) {
    details.push({ label: 'علت مرجوعی', value: data.reason })
  }

  if (type === 'refunded') {
    if (data?.reason) details.push({ label: 'علت Refund', value: data.reason })
    if (typeof data?.amount === 'number') details.push({ label: 'مبلغ Refund', value: `${formatNumber(data.amount)}` })
  }

  if (type === 'linecancelled' && data?.lines) {
    try {
      const lines = Array.isArray(data.lines) ? data.lines : []
      if (lines.length > 0) {
        details.push({
          label: 'آیتم‌ها',
          value: lines.map((l: any) => `${l.skuId} x${l.qty}`).join(' / ')
        })
      }
    } catch {}
  }

  if (type === 'refundrequested' || type === 'refundapproved' || type === 'refundcompleted') {
    if (data?.refundId) details.push({ label: 'کد درخواست', value: String(data.refundId) })
    if (data?.amount) details.push({ label: 'مبلغ', value: `${formatNumber(Number(data.amount))}` })
    if (data?.currency) details.push({ label: 'ارز', value: String(data.currency) })
    if (data?.destination) details.push({ label: 'مقصد', value: String(data.destination) })
  }

  if (type === 'refundrejected' && data?.reason) {
    details.push({ label: 'علت رد', value: String(data.reason) })
  }

  if (type === 'cancelled' && item.message) {
    details.push({ label: 'علت لغو', value: item.message })
  }

  if (type === 'paymentfailed' && item.message) {
    details.push({ label: 'علت خطا', value: item.message })
  }

  if (item.message && details.every(d => d.value !== item.message)) {
    details.push({ label: 'توضیحات', value: item.message })
  }

  if (item.dataJson && details.length === 0) {
    details.push({ label: 'جزئیات فنی', value: item.dataJson })
  }

  return details
}
