import { Spinner } from '@/shared/components/Spinner'
import { useStockLedgerEntryDetails } from '../queries'
import { StockMovementTypeLabels, StockMovementTypeColors } from '../types'

interface StockLedgerEntryDetailsModalProps {
  entryId: string | null
  onClose: () => void
}

export function StockLedgerEntryDetailsModal({ entryId, onClose }: StockLedgerEntryDetailsModalProps) {
  const { data: entry, isLoading, error } = useStockLedgerEntryDetails(entryId || undefined)

  if (!entryId) return null

  function formatDateTime(dateStr: string) {
    try {
      return new Date(dateStr).toLocaleDateString('fa-IR', {
        year: 'numeric',
        month: 'long',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit',
      })
    } catch {
      return dateStr
    }
  }

  function formatDate(dateStr: string | null | undefined) {
    if (!dateStr) return '-'
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

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50" onClick={onClose}>
      <div
        className="relative w-full max-w-4xl max-h-[90vh] overflow-y-auto rounded-xl border border-slate-200 bg-white shadow-xl"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className="sticky top-0 border-b border-slate-200 bg-slate-50 px-6 py-4">
          <div className="flex items-center justify-between">
            <h2 className="text-xl font-semibold text-slate-900">جزئیات تراکنش</h2>
            <button
              onClick={onClose}
              className="rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-slate-200 hover:text-slate-600"
            >
              <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" d="M6 18 18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
        </div>

        {/* Content */}
        <div className="p-6">
          {isLoading ? (
            <div className="flex items-center justify-center py-20">
              <Spinner className="h-8 w-8 text-emerald-600" />
            </div>
          ) : error || !entry ? (
            <div className="rounded-xl border border-red-200 bg-red-50 p-8 text-center">
              <p className="text-lg font-medium text-red-800">خطا در دریافت اطلاعات</p>
              <p className="mt-2 text-sm text-red-600">{(error as Error)?.message || 'رکورد یافت نشد'}</p>
            </div>
          ) : (
            <div className="space-y-6">
              {/* اطلاعات اصلی */}
              <div className="rounded-lg border border-slate-200 bg-slate-50 p-4">
                <h3 className="mb-4 text-lg font-semibold text-slate-900">اطلاعات اصلی</h3>
                <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
                  <div>
                    <label className="text-sm font-medium text-slate-600">تاریخ و زمان</label>
                    <p className="mt-1 text-sm text-slate-900">{formatDateTime(entry.timestamp)}</p>
                  </div>
                  <div>
                    <label className="text-sm font-medium text-slate-600">نوع عملیات</label>
                    <p className="mt-1">
                      <span
                        className={`inline-flex rounded-full px-2.5 py-0.5 text-xs font-medium ${
                          StockMovementTypeColors[entry.movementType]
                        }`}
                      >
                        {StockMovementTypeLabels[entry.movementType]}
                      </span>
                    </p>
                  </div>
                  <div>
                    <label className="text-sm font-medium text-slate-600">مقدار</label>
                    <p
                      className={`mt-1 text-lg font-semibold ${
                        entry.deltaQty > 0 ? 'text-emerald-600' : 'text-red-600'
                      }`}
                    >
                      {entry.deltaQty > 0 ? '+' : ''}
                      {entry.deltaQty.toLocaleString('fa-IR')}
                    </p>
                  </div>
                  <div>
                    <label className="text-sm font-medium text-slate-600">هزینه واحد</label>
                    <p className="mt-1 text-sm text-slate-900">
                      {entry.unitCost ? `${entry.unitCost.toLocaleString('fa-IR')} ریال` : '-'}
                    </p>
                  </div>
                  <div>
                    <label className="text-sm font-medium text-slate-600">انبار</label>
                    <p className="mt-1 text-sm text-slate-900">{entry.warehouseName || '-'}</p>
                  </div>
                  <div>
                    <label className="text-sm font-medium text-slate-600">SKU</label>
                    <p className="mt-1 font-mono text-sm text-slate-900">{entry.sku || '-'}</p>
                  </div>
                  <div>
                    <label className="text-sm font-medium text-slate-600">لات</label>
                    <p className="mt-1 text-sm text-slate-900">{entry.lotNumber || '-'}</p>
                  </div>
                  <div>
                    <label className="text-sm font-medium text-slate-600">تاریخ انقضا</label>
                    <p className="mt-1 text-sm text-slate-900">{formatDate(entry.expiryDate)}</p>
                  </div>
                  {entry.note && (
                    <div className="md:col-span-2">
                      <label className="text-sm font-medium text-slate-600">یادداشت</label>
                      <p className="mt-1 text-sm text-slate-900">{entry.note}</p>
                    </div>
                  )}
                </div>
              </div>

              {/* جزئیات سریال‌ها */}
              {entry.serials && entry.serials.length > 0 && (
                <div className="rounded-lg border border-slate-200 bg-white p-4">
                  <h3 className="mb-4 text-lg font-semibold text-slate-900">Serials</h3>
                  <div className="text-sm text-slate-600">
                    Count: <span className="font-medium text-slate-900">{entry.serials.length.toLocaleString('fa-IR')}</span>
                  </div>
                  <div className="mt-3 grid gap-2 sm:grid-cols-2">
                    {entry.serials.map((serial) => (
                      <div key={serial.serialNumber} className="rounded-lg border border-slate-200 bg-slate-50 px-3 py-2">
                        <div className="font-mono text-sm text-slate-900">{serial.serialNumber}</div>
                        <div className="text-xs text-slate-500">{serial.status}</div>
                      </div>
                    ))}
                  </div>
                </div>
              )}
              {entry.refDocDetails && (
                <div className="rounded-lg border border-slate-200 bg-white p-4">
                  <h3 className="mb-4 text-lg font-semibold text-slate-900">
                    جزئیات سند مرجع ({entry.refDocType})
                  </h3>
                  {entry.refDocType === 'Receipt' && (
                    <ReceiptDetails details={entry.refDocDetails as any} />
                  )}
                  {entry.refDocType === 'Issue' && <IssueDetails details={entry.refDocDetails as any} />}
                  {entry.refDocType === 'Transfer' && (
                    <TransferDetails details={entry.refDocDetails as any} />
                  )}
                  {entry.refDocType === 'Adjustment' && (
                    <AdjustmentDetails details={entry.refDocDetails as any} />
                  )}
                  {entry.refDocType === 'StockMove' && (
                    <StockMoveDetails details={entry.refDocDetails as any} />
                  )}
                </div>
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  )
}

function ReceiptDetails({ details }: { details: any }) {
  return (
    <div className="space-y-4">
      <div className="grid grid-cols-2 gap-4 text-sm">
        <div>
          <span className="font-medium text-slate-600">شناسه:</span>{' '}
          <span className="font-mono text-slate-900">{details.id?.substring(0, 8)}</span>
        </div>
        <div>
          <span className="font-medium text-slate-600">مرجع خارجی:</span>{' '}
          <span className="text-slate-900">{details.externalRef || '-'}</span>
        </div>
        <div>
          <span className="font-medium text-slate-600">تاریخ سند:</span>{' '}
          <span className="text-slate-900">{new Date(details.docDate).toLocaleDateString('fa-IR')}</span>
        </div>
        <div>
          <span className="font-medium text-slate-600">وضعیت:</span>{' '}
          <span className="text-slate-900">{details.status}</span>
        </div>
      </div>
      {details.lines && details.lines.length > 0 && (
        <div>
          <h4 className="mb-2 text-sm font-semibold text-slate-700">خطوط رسید</h4>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-slate-50">
                <tr>
                  <th className="px-3 py-2 text-right">ردیف</th>
                  <th className="px-3 py-2 text-right">مقدار</th>
                  <th className="px-3 py-2 text-right">لات</th>
                  <th className="px-3 py-2 text-right">تاریخ انقضا</th>
                  <th className="px-3 py-2 text-right">هزینه واحد</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200">
                {details.lines.map((line: any) => (
                  <tr key={line.id}>
                    <td className="px-3 py-2">{line.lineNo}</td>
                    <td className="px-3 py-2">{line.qty.toLocaleString('fa-IR')}</td>
                    <td className="px-3 py-2">{line.lotNumber || '-'}</td>
                    <td className="px-3 py-2">
                      {line.expiryDate ? new Date(line.expiryDate).toLocaleDateString('fa-IR') : '-'}
                    </td>
                    <td className="px-3 py-2">
                      {line.unitCost ? `${line.unitCost.toLocaleString('fa-IR')} ریال` : '-'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  )
}

function IssueDetails({ details }: { details: any }) {
  return (
    <div className="space-y-4">
      <div className="grid grid-cols-2 gap-4 text-sm">
        <div>
          <span className="font-medium text-slate-600">شناسه:</span>{' '}
          <span className="font-mono text-slate-900">{details.id?.substring(0, 8)}</span>
        </div>
        <div>
          <span className="font-medium text-slate-600">مرجع خارجی:</span>{' '}
          <span className="text-slate-900">{details.externalRef || '-'}</span>
        </div>
        <div>
          <span className="font-medium text-slate-600">تاریخ سند:</span>{' '}
          <span className="text-slate-900">{new Date(details.docDate).toLocaleDateString('fa-IR')}</span>
        </div>
        <div>
          <span className="font-medium text-slate-600">وضعیت:</span>{' '}
          <span className="text-slate-900">{details.status}</span>
        </div>
      </div>
      {details.lines && details.lines.length > 0 && (
        <div>
          <h4 className="mb-2 text-sm font-semibold text-slate-700">خطوط خروجی</h4>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-slate-50">
                <tr>
                  <th className="px-3 py-2 text-right">ردیف</th>
                  <th className="px-3 py-2 text-right">مقدار درخواستی</th>
                  <th className="px-3 py-2 text-right">تخصیص‌ها</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200">
                {details.lines.map((line: any) => (
                  <tr key={line.id}>
                    <td className="px-3 py-2">{line.lineNo}</td>
                    <td className="px-3 py-2">{line.requestedQty.toLocaleString('fa-IR')}</td>
                    <td className="px-3 py-2">
                      {line.allocations && line.allocations.length > 0 ? (
                        <div className="space-y-1">
                          {line.allocations.map((alloc: any) => (
                            <div key={alloc.id} className="text-xs">
                              مقدار: {alloc.qty.toLocaleString('fa-IR')}
                            </div>
                          ))}
                        </div>
                      ) : (
                        '-'
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  )
}

function TransferDetails({ details }: { details: any }) {
  return (
    <div className="space-y-4">
      <div className="grid grid-cols-2 gap-4 text-sm">
        <div>
          <span className="font-medium text-slate-600">شناسه:</span>{' '}
          <span className="font-mono text-slate-900">{details.id?.substring(0, 8)}</span>
        </div>
        <div>
          <span className="font-medium text-slate-600">مرجع خارجی:</span>{' '}
          <span className="text-slate-900">{details.externalRef || '-'}</span>
        </div>
        <div>
          <span className="font-medium text-slate-600">تاریخ سند:</span>{' '}
          <span className="text-slate-900">{new Date(details.docDate).toLocaleDateString('fa-IR')}</span>
        </div>
        <div>
          <span className="font-medium text-slate-600">وضعیت:</span>{' '}
          <span className="text-slate-900">{details.status}</span>
        </div>
      </div>
      {details.lines && details.lines.length > 0 && (
        <div>
          <h4 className="mb-2 text-sm font-semibold text-slate-700">خطوط انتقال</h4>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-slate-50">
                <tr>
                  <th className="px-3 py-2 text-right">ردیف</th>
                  <th className="px-3 py-2 text-right">مقدار درخواستی</th>
                  <th className="px-3 py-2 text-right">مقدار تخصیص یافته</th>
                  <th className="px-3 py-2 text-right">سگمنت‌ها</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200">
                {details.lines.map((line: any) => (
                  <tr key={line.id}>
                    <td className="px-3 py-2">{line.lineNo}</td>
                    <td className="px-3 py-2">{line.requestedQty.toLocaleString('fa-IR')}</td>
                    <td className="px-3 py-2">{line.allocatedQty.toLocaleString('fa-IR')}</td>
                    <td className="px-3 py-2">
                      {line.segments && line.segments.length > 0 ? (
                        <div className="space-y-1">
                          {line.segments.map((seg: any) => (
                            <div key={seg.id} className="text-xs">
                              مقدار: {seg.qty.toLocaleString('fa-IR')}, دریافت: {seg.receivedQty.toLocaleString('fa-IR')}
                            </div>
                          ))}
                        </div>
                      ) : (
                        '-'
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  )
}

function AdjustmentDetails({ details }: { details: any }) {
  return (
    <div className="space-y-4">
      <div className="grid grid-cols-2 gap-4 text-sm">
        <div>
          <span className="font-medium text-slate-600">شناسه:</span>{' '}
          <span className="font-mono text-slate-900">{details.id?.substring(0, 8)}</span>
        </div>
        <div>
          <span className="font-medium text-slate-600">دلیل:</span>{' '}
          <span className="text-slate-900">{details.reason || '-'}</span>
        </div>
        <div>
          <span className="font-medium text-slate-600">تاریخ سند:</span>{' '}
          <span className="text-slate-900">{new Date(details.docDate).toLocaleDateString('fa-IR')}</span>
        </div>
        <div>
          <span className="font-medium text-slate-600">وضعیت:</span>{' '}
          <span className="text-slate-900">{details.status}</span>
        </div>
        {details.note && (
          <div className="md:col-span-2">
            <span className="font-medium text-slate-600">یادداشت:</span>{' '}
            <span className="text-slate-900">{details.note}</span>
          </div>
        )}
      </div>
      {details.lines && details.lines.length > 0 && (
        <div>
          <h4 className="mb-2 text-sm font-semibold text-slate-700">خطوط اصلاح</h4>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-slate-50">
                <tr>
                  <th className="px-3 py-2 text-right">ردیف</th>
                  <th className="px-3 py-2 text-right">تغییر مقدار</th>
                  <th className="px-3 py-2 text-right">لات</th>
                  <th className="px-3 py-2 text-right">تاریخ انقضا</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200">
                {details.lines.map((line: any) => (
                  <tr key={line.id}>
                    <td className="px-3 py-2">{line.lineNo}</td>
                    <td
                      className={`px-3 py-2 font-semibold ${
                        line.qtyDelta > 0 ? 'text-emerald-600' : 'text-red-600'
                      }`}
                    >
                      {line.qtyDelta > 0 ? '+' : ''}
                      {line.qtyDelta.toLocaleString('fa-IR')}
                    </td>
                    <td className="px-3 py-2">{line.lotNumber || '-'}</td>
                    <td className="px-3 py-2">
                      {line.expiryDate ? new Date(line.expiryDate).toLocaleDateString('fa-IR') : '-'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  )
}

function StockMoveDetails({ details }: { details: any }) {
  return (
    <div className="space-y-4">
      <div className="grid grid-cols-2 gap-4 text-sm">
        <div>
          <span className="font-medium text-slate-600">شناسه StockItem:</span>{' '}
          <span className="font-mono text-slate-900">{details.id?.substring(0, 8)}</span>
        </div>
        <div>
          <span className="font-medium text-slate-600">SKU:</span>{' '}
          <span className="font-mono text-slate-900">{details.sku || '-'}</span>
        </div>
        <div>
          <span className="font-medium text-slate-600">لات:</span>{' '}
          <span className="text-slate-900">{details.lotNumber || '-'}</span>
        </div>
        <div>
          <span className="font-medium text-slate-600">تاریخ انقضا:</span>{' '}
          <span className="text-slate-900">
            {details.expiryDate ? new Date(details.expiryDate).toLocaleDateString('fa-IR') : '-'}
          </span>
        </div>
        <div>
          <span className="font-medium text-slate-600">موجودی کل:</span>{' '}
          <span className="text-slate-900">{details.onHand.toLocaleString('fa-IR')}</span>
        </div>
        <div>
          <span className="font-medium text-slate-600">موجودی آزاد:</span>{' '}
          <span className="text-slate-900">{details.available.toLocaleString('fa-IR')}</span>
        </div>
        <div>
          <span className="font-medium text-slate-600">رزرو شده:</span>{' '}
          <span className="text-slate-900">{details.reserved.toLocaleString('fa-IR')}</span>
        </div>
        <div>
          <span className="font-medium text-slate-600">مسدود شده:</span>{' '}
          <span className="text-slate-900">{details.blocked.toLocaleString('fa-IR')}</span>
        </div>
      </div>
    </div>
  )
}



