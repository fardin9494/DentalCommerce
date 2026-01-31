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
  const [actionNote, setActionNote] = useState('')
  const [trackingCode, setTrackingCode] = useState('')
  const [carrier, setCarrier] = useState('')
  const [returnReason, setReturnReason] = useState('')
  const [refundReason, setRefundReason] = useState('')
  const [refundAmount, setRefundAmount] = useState<string>('')
  const [storeInfo, setStoreInfo] = useState<StoreInfo | null>(null)
  const [productInfoMap, setProductInfoMap] = useState<Map<string, ProductInfo>>(new Map())
  const [stockItemInfoMap, setStockItemInfoMap] = useState<Map<string, StockItemInfo>>(new Map())

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

  const handleAction = async (action: 'ship' | 'deliver' | 'return' | 'refund') => {
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
      }
      setActionNote('')
      await Promise.all([loadTimeline(), refreshOrder(id)])
    } catch (err: any) {
      const msg = err?.message || 'خطا در انجام عملیات'
      toast.error(msg)
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
                <input className="input" value={carrier} onChange={e => setCarrier(e.target.value)} placeholder="پست، تیپاکس..." />
              </div>
              <div>
                <label className="label">Tracking Code</label>
                <input className="input" value={trackingCode} onChange={e => setTrackingCode(e.target.value)} placeholder="کد رهگیری" />
              </div>
              <div>
                <label className="label">یادداشت</label>
                <input className="input" value={actionNote} onChange={e => setActionNote(e.target.value)} placeholder="یادداشت اختیاری" />
              </div>
              <div>
                <label className="label">علت مرجوعی</label>
                <input className="input" value={returnReason} onChange={e => setReturnReason(e.target.value)} placeholder="دلیل مرجوعی" />
              </div>
              <div>
                <label className="label">علت Refund</label>
                <input className="input" value={refundReason} onChange={e => setRefundReason(e.target.value)} placeholder="دلیل Refund" />
              </div>
              <div>
                <label className="label">مبلغ Refund (اختیاری)</label>
                <input className="input" value={refundAmount} onChange={e => setRefundAmount(e.target.value)} placeholder="مبلغ" />
              </div>
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
              {!canShip && !canDeliver && !canReturn && !canRefund && (
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
                        <td className="px-4 py-3 text-center font-medium">{line.quantity}</td>
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

          {/* Timeline */}
          <div className="card p-6">
            <h3 className="text-lg font-semibold text-gray-900 mb-4 flex items-center gap-2">
              <svg className="w-5 h-5 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              Order Timeline
            </h3>
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
              {timeline.map((item, idx) => (
                <div key={item.id} className="relative pl-8 border-r-2 border-gray-200 last:border-0 pb-4 last:pb-0">
                  <div className="absolute -right-2 top-0 w-4 h-4 bg-emerald-500 rounded-full border-2 border-white"></div>
                  <div className="bg-gray-50 rounded-lg p-4 hover:bg-gray-100 transition-colors">
                    <div className="flex flex-wrap items-center justify-between gap-2 mb-2">
                      <span className="font-semibold text-gray-900">{item.eventType}</span>
                      <span className="text-xs text-gray-500">{formatDate(item.createdAt)}</span>
                    </div>
                    {(item.fromStatus || item.toStatus) && (
                      <div className="text-xs text-gray-600 mb-2 flex items-center gap-2">
                        {item.fromStatus && (
                          <span className="badge badge-gray">{item.fromStatus}</span>
                        )}
                        {item.fromStatus && item.toStatus && (
                          <svg className="w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                          </svg>
                        )}
                        {item.toStatus && (
                          <span className="badge badge-green">{item.toStatus}</span>
                        )}
                      </div>
                    )}
                    {item.message && (
                      <div className="mt-2 text-sm text-gray-700 bg-white p-2 rounded border">
                        {item.message}
                      </div>
                    )}
                    {item.dataJson && (
                      <pre className="mt-2 text-xs whitespace-pre-wrap bg-white p-3 rounded border font-mono max-h-48 overflow-auto">
                        {item.dataJson}
                      </pre>
                    )}
                  </div>
                </div>
              ))}
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
