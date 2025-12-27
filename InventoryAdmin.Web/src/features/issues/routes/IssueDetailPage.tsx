import { useState, useMemo, useRef, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { PageHeader } from '@/shared/components/PageHeader'
import { Spinner } from '@/shared/components/Spinner'
import { useIssue, useAddIssueLine, useRemoveIssueLine, useAllocateIssueLineFefo, useAllocateIssueLineFifo, useAllocateIssueLineLifo, usePostIssue, useCancelIssue } from '../queries'
import { useConfirm } from '@/shared/components/confirm/ConfirmProvider'
import { AllocateMethodModal } from '../components/AllocateMethodModal'
import { ProductSearchSelect, type ProductSelection } from '@/shared/components/ProductSearchSelect'
import { useActiveWarehouses } from '@/shared/hooks/useWarehouses'
import { useProductNames } from '@/shared/hooks/useProductNames'
import { useWarehouseNames } from '@/shared/hooks/useWarehouses'
import { useProductDetail } from '@/shared/hooks/useProducts'
import { issueDisplayRef } from '@/shared/utils/inventoryDocumentReference'

const statusLabels: Record<string, string> = {
  Draft: 'پیش‌نویس',
  Posted: 'ثبت شده',
  Canceled: 'لغو شده',
}

const statusColors: Record<string, string> = {
  Draft: 'bg-amber-100 text-amber-700',
  Posted: 'bg-green-100 text-green-700',
  Canceled: 'bg-red-100 text-red-700',
}

export function IssueDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { data: issue, isLoading, error } = useIssue(id)
  const { data: warehouses } = useActiveWarehouses()
  const { getWarehouseName } = useWarehouseNames()
  const addLine = useAddIssueLine(id!)
  const removeLine = useRemoveIssueLine(id!)
  const allocateFefo = useAllocateIssueLineFefo(id!)
  const allocateFifo = useAllocateIssueLineFifo(id!)
  const allocateLifo = useAllocateIssueLineLifo(id!)
  const post = usePostIssue(id!)
  const cancel = useCancelIssue(id!)
  const confirm = useConfirm()
  const [allocateModalOpen, setAllocateModalOpen] = useState(false)
  const [selectedLineId, setSelectedLineId] = useState<string | null>(null)
  const [expandedLineId, setExpandedLineId] = useState<string | null>(null)

  // Inline add line state
  const [isAddingNewLine, setIsAddingNewLine] = useState(false)
  const [newLineData, setNewLineData] = useState<{
    productId?: string
    variantId?: string
    productName?: string
    variantValue?: string
    qty: string
  }>({
    qty: '1',
  })

  // Ref for quantity input auto-focus
  const qtyInputRef = useRef<HTMLInputElement>(null)

  // Get product detail for variant selection when adding new line
  const { data: newLineProductDetail } = useProductDetail(newLineData.productId, !!newLineData.productId && isAddingNewLine)

  // جمع‌آوری productId های تمام خطوط برای fetch کردن نام محصولات
  const productIds = useMemo(() => issue?.lines.map((line) => line.productId) || [], [issue?.lines])
  const { getProductName, getVariantName } = useProductNames(productIds)

  // Auto-focus on quantity input when product is selected
  useEffect(() => {
    if (newLineData.productId && qtyInputRef.current) {
      const timer = setTimeout(() => {
        qtyInputRef.current?.focus()
        qtyInputRef.current?.select()
      }, 100)
      return () => clearTimeout(timer)
    }
  }, [newLineData.productId])

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-20">
        <Spinner className="h-8 w-8 text-emerald-600" />
      </div>
    )
  }

  if (error || !issue) {
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
        <p className="text-lg font-medium text-red-800">خروجی پیدا نشد</p>
        <p className="mt-2 text-sm text-red-600">{(error as Error)?.message || 'خطا در دریافت اطلاعات'}</p>
        <button
          onClick={() => navigate('/issues')}
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

  function handleStartAddLine() {
    setIsAddingNewLine(true)
    setNewLineData({ qty: '1' })
  }

  function handleCancelAddLine() {
    setIsAddingNewLine(false)
    setNewLineData({ qty: '1' })
  }

  function handleProductSelect(selection: ProductSelection) {
    setNewLineData((prev) => ({
      ...prev,
      productId: selection.productId,
      variantId: selection.variantId,
      productName: selection.productName,
      variantValue: selection.variantValue,
    }))
  }

  async function handleSaveNewLine() {
    if (!newLineData.productId) return

    const qtyNum = parseFloat(newLineData.qty)
    if (isNaN(qtyNum) || qtyNum <= 0) return

    // Check if product has variants and one is required
    const activeVariants = newLineProductDetail?.variants?.filter(v => v.isActive) || []
    if (activeVariants.length > 0 && !newLineData.variantId) {
      return // Don't allow save without variant selection
    }

    await addLine.mutateAsync({
      productId: newLineData.productId,
      variantId: newLineData.variantId || null,
      qty: qtyNum,
    })

    // Reset for next line
    setNewLineData({ qty: '1' })
    setIsAddingNewLine(false)
  }

  // Handle keyboard shortcuts in inline form
  function handleInlineFormKeyDown(e: React.KeyboardEvent) {
    if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) {
      e.preventDefault()
      handleSaveNewLine()
    }
    if (e.key === 'Escape') {
      e.preventDefault()
      handleCancelAddLine()
    }
  }

  async function handleRemoveLine(lineId: string) {
    const ok = await confirm.confirm({ title: 'حذف خط', message: 'آیا از حذف این خط مطمئن هستید؟' })
    if (!ok) return
    await removeLine.mutateAsync(lineId)
  }

  function handleOpenAllocateModal(lineId: string) {
    setSelectedLineId(lineId)
    setAllocateModalOpen(true)
  }

  async function handleAllocate(method: 'fefo' | 'fifo' | 'lifo', preferredWarehouseId?: string) {
    if (!selectedLineId) return

    const line = issue?.lines.find(l => l.id === selectedLineId)
    if (!line) return

    const methodLabels = {
      fefo: 'FEFO',
      fifo: 'FIFO',
      lifo: 'LIFO',
    }

    const warehouseName = preferredWarehouseId
      ? warehouses?.find(w => w.id === preferredWarehouseId)?.name
      : 'همه انبارها'

    const ok = await confirm.confirm({
      title: `تخصیص ${methodLabels[method]}`,
      message: `آیا می‌خواهید خط ${line.lineNo} را با روش ${methodLabels[method]} ${preferredWarehouseId ? `از انبار ${warehouseName}` : 'از تمام انبارها'} تخصیص دهید؟`
    })
    if (!ok) return

    try {
      switch (method) {
        case 'fefo':
          await allocateFefo.mutateAsync({ lineId: selectedLineId, preferredWarehouseId })
          break
        case 'fifo':
          await allocateFifo.mutateAsync({ lineId: selectedLineId, preferredWarehouseId })
          break
        case 'lifo':
          await allocateLifo.mutateAsync({ lineId: selectedLineId, preferredWarehouseId })
          break
      }
    } finally {
      setAllocateModalOpen(false)
      setSelectedLineId(null)
    }
  }

  async function handlePost() {
    const ok = await confirm.confirm({ title: 'ثبت خروجی', message: 'آیا می‌خواهید این خروجی را ثبت کنید؟ کالاها از انبار خارج خواهند شد.' })
    if (!ok) return
    await post.mutateAsync()
  }

  async function handleCancel() {
    const ok = await confirm.confirm({ title: 'لغو خروجی', message: 'آیا می‌خواهید این خروجی را لغو کنید؟ این عملیات قابل بازگشت نیست.' })
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

  const isDraft = issue.status === 'Draft'
  const totalRequested = issue.lines.reduce((sum, l) => sum + l.requestedQty, 0)
  const totalAllocated = issue.lines.reduce((sum, l) => sum + l.allocatedQty, 0)
  const totalRemaining = issue.lines.reduce((sum, l) => sum + l.remainingQty, 0)

  return (
    <div className="space-y-6">
      {/* Header */}
      <PageHeader
        title={`خروجی #${issue.id.substring(0, 8)}`}
        actions={
          <div className="flex flex-wrap items-center gap-2">
            {isDraft && (
              <>
                <button
                  onClick={handleStartAddLine}
                  disabled={isAddingNewLine}
                  className="inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-4 py-2 text-sm font-medium text-white shadow-sm transition-colors hover:bg-emerald-700 disabled:opacity-50"
                >
                  <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                  </svg>
                  افزودن خط
                </button>
                <button
                  onClick={handlePost}
                  disabled={post.isPending || issue.lines.length === 0 || totalRemaining > 0}
                  className="inline-flex items-center gap-2 rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white shadow-sm transition-colors hover:bg-blue-700 disabled:opacity-50"
                  title={totalRemaining > 0 ? 'ابتدا همه خطوط را تخصیص دهید' : undefined}
                >
                  <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" d="m4.5 12.75 6 6 9-13.5" />
                  </svg>
                  ثبت خروجی
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
              onClick={() => navigate('/issues')}
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
        مدیریت خروج کالا از انبار
      </PageHeader>

      {/* Info Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        {/* Status Card */}
        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
          <div className="text-sm font-medium text-slate-500">وضعیت</div>
          <div className="mt-2">
            <span
              className={`inline-flex items-center rounded-full px-3 py-1 text-sm font-medium ${statusColors[issue.status] || 'bg-slate-100 text-slate-700'
                }`}
            >
              {statusLabels[issue.status] || issue.status}
            </span>
          </div>
        </div>

        {/* Warehouse Card */}
        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
          <div className="text-sm font-medium text-slate-500">انبار</div>
          <div className="mt-2 text-lg font-semibold text-slate-900">
            {issue.warehouseId
              ? (warehouses?.find((w) => w.id === issue.warehouseId)?.name || getWarehouseName(issue.warehouseId))
              : 'تعیین نشده'}
          </div>
        </div>

        {/* Doc Date Card */}
        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
          <div className="text-sm font-medium text-slate-500">تاریخ سند</div>
          <div className="mt-2 text-lg font-semibold text-slate-900">{formatDate(issue.docDate)}</div>
        </div>

        {/* External Ref Card */}
        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
          <div className="text-sm font-medium text-slate-500">شماره مرجع</div>
          <div className="mt-2 text-lg font-semibold text-slate-900">{issueDisplayRef(issue.id, issue.externalRef)}</div>
        </div>
      </div>

      {/* Summary Stats */}
      <div className="grid gap-4 sm:grid-cols-3">
        <div className="rounded-xl border border-blue-200 bg-blue-50 p-4 shadow-sm">
          <div className="text-sm font-medium text-blue-600">مجموع درخواستی</div>
          <div className="mt-2 text-2xl font-bold text-blue-700">{totalRequested.toLocaleString('fa-IR')}</div>
        </div>
        <div className="rounded-xl border border-emerald-200 bg-emerald-50 p-4 shadow-sm">
          <div className="text-sm font-medium text-emerald-600">تخصیص یافته</div>
          <div className="mt-2 text-2xl font-bold text-emerald-700">{totalAllocated.toLocaleString('fa-IR')}</div>
        </div>
        <div className={`rounded-xl border p-4 shadow-sm ${totalRemaining > 0 ? 'border-orange-200 bg-orange-50' : 'border-slate-200 bg-slate-50'}`}>
          <div className={`text-sm font-medium ${totalRemaining > 0 ? 'text-orange-600' : 'text-slate-500'}`}>باقیمانده</div>
          <div className={`mt-2 text-2xl font-bold ${totalRemaining > 0 ? 'text-orange-700' : 'text-slate-600'}`}>
            {totalRemaining.toLocaleString('fa-IR')}
          </div>
        </div>
      </div>

      {/* Lines Table */}
      <div className="rounded-xl border border-slate-200 bg-white shadow-sm overflow-hidden">
        <div className="flex items-center justify-between border-b border-slate-200 bg-slate-50 px-6 py-4">
          <h3 className="text-lg font-semibold text-slate-900">
            خطوط خروجی
            <span className="mr-2 rounded-full bg-slate-200 px-2.5 py-0.5 text-sm font-medium text-slate-600">
              {issue.lines.length}
            </span>
          </h3>
          {isDraft && (
            <button
              onClick={handleStartAddLine}
              disabled={isAddingNewLine}
              className="inline-flex items-center gap-1.5 rounded-lg bg-emerald-600 px-3 py-1.5 text-sm font-medium text-white transition-colors hover:bg-emerald-700 disabled:opacity-50"
            >
              <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
              </svg>
              افزودن
            </button>
          )}
        </div>

        {issue.lines.length === 0 && !isAddingNewLine ? (
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
                  <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600 text-center">درخواستی</th>
                  <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600 text-center">تخصیص یافته</th>
                  <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600 text-center">باقیمانده</th>
                  <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">عملیات</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {/* Inline Add New Line Row */}
                {isDraft && isAddingNewLine && (
                  <tr className="bg-emerald-50/50 border-2 border-emerald-200" onKeyDown={handleInlineFormKeyDown}>
                    <td className="px-4 py-3 text-slate-500 font-medium">جدید</td>
                    {!newLineData.productId ? (
                      <td className="px-4 py-4" colSpan={5}>
                        <div className="min-w-[400px]">
                          <ProductSearchSelect
                            onSelect={handleProductSelect}
                            onCancel={handleCancelAddLine}
                            autoConfirm={true}
                          />
                        </div>
                      </td>
                    ) : (
                      <>
                        <td className="px-4 py-3">
                          <div className="font-medium text-slate-900">{newLineData.productName}</div>
                        </td>
                        <td className="px-4 py-3">
                          {(() => {
                            const activeVariants = newLineProductDetail?.variants?.filter(v => v.isActive) || []
                            if (activeVariants.length > 0) {
                              return (
                                <div className="space-y-1.5">
                                  <div className="flex flex-wrap gap-1.5">
                                    {activeVariants.map((variant) => (
                                      <button
                                        key={variant.id}
                                        type="button"
                                        onClick={() => setNewLineData(prev => ({ ...prev, variantId: variant.id, variantValue: variant.value }))}
                                        className={`rounded-md px-2.5 py-1 text-xs font-medium transition-colors ${newLineData.variantId === variant.id
                                            ? 'bg-emerald-600 text-white'
                                            : 'bg-white border border-slate-300 text-slate-700 hover:bg-slate-50'
                                          }`}
                                      >
                                        {variant.value}
                                      </button>
                                    ))}
                                  </div>
                                </div>
                              )
                            }
                            return <span className="text-slate-400 text-sm">-</span>
                          })()}
                        </td>
                        <td className="px-4 py-3 text-center">
                          <input
                            ref={qtyInputRef}
                            type="number"
                            value={newLineData.qty}
                            onChange={(e) => setNewLineData(prev => ({ ...prev, qty: e.target.value }))}
                            min="0.01"
                            step="0.01"
                            className="w-24 rounded-lg border border-slate-300 px-3 py-2 text-sm text-center focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20"
                            placeholder="تعداد"
                          />
                        </td>
                        <td className="px-4 py-3 text-center text-slate-400">-</td>
                        <td className="px-4 py-3 text-center text-slate-400">-</td>
                      </>
                    )}
                    <td className="px-4 py-3">
                      {newLineData.productId ? (
                        <div className="flex items-center gap-1.5">
                          <button
                            onClick={handleSaveNewLine}
                            disabled={addLine.isPending || !newLineData.productId || parseFloat(newLineData.qty) <= 0 || (newLineProductDetail?.variants?.filter(v => v.isActive).length || 0) > 0 && !newLineData.variantId}
                            className="inline-flex items-center gap-1.5 rounded-lg bg-emerald-600 px-3 py-1.5 text-xs font-medium text-white transition-colors hover:bg-emerald-700 disabled:opacity-50"
                            title="Ctrl+Enter برای ثبت سریع"
                          >
                            {addLine.isPending ? (
                              <>
                                <Spinner className="h-3 w-3" />
                                در حال ثبت...
                              </>
                            ) : (
                              <>
                                <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                                  <path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" />
                                </svg>
                                ثبت
                              </>
                            )}
                          </button>
                          <button
                            onClick={handleCancelAddLine}
                            disabled={addLine.isPending}
                            className="rounded-lg border border-slate-300 bg-white px-3 py-1.5 text-xs font-medium text-slate-700 transition-colors hover:bg-slate-50 disabled:opacity-50"
                            title="Escape برای انصراف"
                          >
                            انصراف
                          </button>
                        </div>
                      ) : (
                        <button
                          onClick={handleCancelAddLine}
                          className="rounded-lg border border-slate-300 bg-white px-3 py-1.5 text-xs font-medium text-slate-700 transition-colors hover:bg-slate-50"
                        >
                          انصراف
                        </button>
                      )}
                    </td>
                  </tr>
                )}
                {issue.lines.map((line) => (
                  <>
                    <tr key={line.id} className="transition-colors hover:bg-slate-50">
                      <td className="whitespace-nowrap px-4 py-3 text-slate-700">{line.lineNo}</td>
                      <td className="whitespace-nowrap px-4 py-3">
                        <div className="font-medium text-slate-900">{getProductName(line.productId)}</div>
                      </td>
                      <td className="whitespace-nowrap px-4 py-3">
                        {line.variantId ? (
                          <span className="inline-flex items-center rounded-md bg-emerald-100 px-2 py-1 text-xs font-medium text-emerald-700">
                            {getVariantName(line.variantId) || line.variantId.slice(0, 8) + '...'}
                          </span>
                        ) : (
                          <span className="text-slate-400">-</span>
                        )}
                      </td>
                      <td className="whitespace-nowrap px-4 py-3 text-center font-medium text-slate-900">
                        {line.requestedQty.toLocaleString('fa-IR')}
                      </td>
                      <td className="whitespace-nowrap px-4 py-3 text-center">
                        <span className="inline-flex items-center rounded-full bg-green-100 px-2.5 py-0.5 text-xs font-medium text-green-700">
                          {line.allocatedQty.toLocaleString('fa-IR')}
                        </span>
                      </td>
                      <td className="whitespace-nowrap px-4 py-3 text-center">
                        <span
                          className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${line.remainingQty > 0
                              ? 'bg-orange-100 text-orange-700'
                              : 'bg-slate-100 text-slate-600'
                            }`}
                        >
                          {line.remainingQty.toLocaleString('fa-IR')}
                        </span>
                      </td>
                      <td className="whitespace-nowrap px-4 py-3">
                        <div className="flex items-center gap-1">
                          {line.allocations.length > 0 && (
                            <button
                              onClick={() => setExpandedLineId(expandedLineId === line.id ? null : line.id)}
                              className="rounded-lg p-1.5 text-blue-600 transition-colors hover:bg-blue-50 hover:text-blue-700"
                              title="مشاهده جزئیات تخصیص"
                            >
                              <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
                                <path strokeLinecap="round" strokeLinejoin="round" d={expandedLineId === line.id ? "m4.5 15.75 7.5-7.5 7.5 7.5" : "m19.5 8.25-7.5 7.5-7.5-7.5"} />
                              </svg>
                            </button>
                          )}
                          {isDraft && (
                            <>
                              <button
                                onClick={() => handleOpenAllocateModal(line.id)}
                                className="rounded-lg p-1.5 text-emerald-600 transition-colors hover:bg-emerald-50 hover:text-emerald-700"
                                title="تخصیص موجودی"
                              >
                                <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
                                  <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 16.875h3.375m0 0h3.375m-3.375 0V13.5m0 3.375v3.375M6 10.5h2.25a2.25 2.25 0 0 0 2.25-2.25V6a2.25 2.25 0 0 0-2.25-2.25H6A2.25 2.25 0 0 0 3.75 6v2.25A2.25 2.25 0 0 0 6 10.5Zm0 9.75h2.25A2.25 2.25 0 0 0 10.5 18v-2.25a2.25 2.25 0 0 0-2.25-2.25H6a2.25 2.25 0 0 0-2.25 2.25V18A2.25 2.25 0 0 0 6 19.5Zm9.75-9.75H18a2.25 2.25 0 0 0 2.25-2.25V6A2.25 2.25 0 0 0 18 3.75h-2.25A2.25 2.25 0 0 0 13.5 6v2.25a2.25 2.25 0 0 0 2.25 2.25Z" />
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
                            </>
                          )}
                        </div>
                      </td>
                    </tr>
                    {/* Allocations Details */}
                    {expandedLineId === line.id && line.allocations.length > 0 && (
                      <tr>
                        <td colSpan={7} className="bg-slate-50 px-4 py-4">
                          <h4 className="font-medium mb-3 text-sm text-slate-700">جزئیات تخصیص‌ها:</h4>
                          <div className="overflow-x-auto rounded-lg border border-slate-200 bg-white">
                            <table className="w-full text-xs">
                              <thead>
                                <tr className="border-b bg-slate-100">
                                  <th className="text-right p-2 font-medium text-slate-600">SKU</th>
                                  <th className="text-right p-2 font-medium text-slate-600">شماره لات</th>
                                  <th className="text-right p-2 font-medium text-slate-600">تاریخ انقضا</th>
                                  <th className="text-right p-2 font-medium text-slate-600">انبار</th>
                                  <th className="text-right p-2 font-medium text-slate-600">قفسه</th>
                                  <th className="text-right p-2 font-medium text-slate-600">مقدار</th>
                                  <th className="text-right p-2 font-medium text-slate-600">Serials</th>
                                </tr>
                              </thead>
                              <tbody className="divide-y divide-slate-100">
                                {line.allocations.map((alloc) => (
                                  <tr key={alloc.id} className="hover:bg-slate-50">
                                    <td className="p-2 font-mono">{alloc.sku || '-'}</td>
                                    <td className="p-2">{alloc.lotNumber || '-'}</td>
                                    <td className="p-2">
                                      {alloc.expiryDate ? new Date(alloc.expiryDate).toLocaleDateString('fa-IR') : '-'}
                                    </td>
                                    <td className="p-2">
                                      {alloc.warehouseName ? (
                                        <span className="inline-flex items-center gap-1 rounded-full bg-blue-100 px-2 py-0.5 text-xs font-medium text-blue-700">
                                          {alloc.warehouseName}
                                        </span>
                                      ) : (
                                        '-'
                                      )}
                                    </td>
                                    <td className="p-2">
                                      {alloc.shelfName ? (
                                        <span className="inline-flex items-center gap-1 rounded-full bg-emerald-100 px-2 py-0.5 text-xs font-medium text-emerald-700">
                                          {alloc.shelfName}
                                        </span>
                                      ) : (
                                        '-'
                                      )}
                                    </td>
                                    <td className="p-2 font-medium">{alloc.qty.toLocaleString('fa-IR')}</td>
                                    <td className="p-2">
                                       {alloc.serials && alloc.serials.length > 0 ? (
                                         <div className="max-w-xs whitespace-normal break-words font-mono text-[11px] text-slate-700">
                                           {alloc.serials.map((s) => s.serialNumber).join(", ")}
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
                        </td>
                      </tr>
                    )}
                  </>
                ))}
              </tbody>
              {/* Summary Footer */}
              <tfoot className="border-t-2 border-slate-200 bg-slate-50">
                <tr>
                  <td colSpan={3} className="px-4 py-3 text-left font-semibold text-slate-700">
                    جمع کل
                  </td>
                  <td className="px-4 py-3 text-center font-bold text-slate-900">
                    {totalRequested.toLocaleString('fa-IR')}
                  </td>
                  <td className="px-4 py-3 text-center font-bold text-green-600">
                    {totalAllocated.toLocaleString('fa-IR')}
                  </td>
                  <td className={`px-4 py-3 text-center font-bold ${totalRemaining > 0 ? 'text-orange-600' : 'text-slate-600'}`}>
                    {totalRemaining.toLocaleString('fa-IR')}
                  </td>
                  <td></td>
                </tr>
              </tfoot>
            </table>
          </div>
        )}
      </div>

      {/* Allocate Modal */}
      {selectedLineId && (
        <AllocateMethodModal
          isOpen={allocateModalOpen}
          onClose={() => {
            setAllocateModalOpen(false)
            setSelectedLineId(null)
          }}
          onSelect={handleAllocate}
          lineNo={issue?.lines.find(l => l.id === selectedLineId)?.lineNo || 0}
          isAllocating={allocateFefo.isPending || allocateFifo.isPending || allocateLifo.isPending}
          defaultWarehouseId={issue?.warehouseId || undefined}
        />
      )}
    </div>
  )
}





