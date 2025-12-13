import { useState, useMemo } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { useQueries } from '@tanstack/react-query'
import { PageHeader } from '@/shared/components/PageHeader'
import { Spinner } from '@/shared/components/Spinner'
import { getIssue } from '../api'
import { useConfirm } from '@/shared/components/confirm/ConfirmProvider'
import { useToast } from '@/shared/components/toast/ToastProvider'

interface PickingItem {
  allocationId: string
  issueId: string
  issueExternalRef: string | null
  lineNo: number
  productId: string
  variantId: string | null
  stockItemId: string
  qty: number
  sku: string | null
  lotNumber: string | null
  expiryDate: string | null
  shelfId: string | null
  shelfName: string | null
  warehouseId: string | null
  warehouseName: string | null
  productName?: string
  variantValue?: string
}

export function PickingPlanPage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const confirm = useConfirm()
  const toast = useToast()
  
  // Get issue IDs from URL params
  const issueIds = useMemo(() => {
    const ids = searchParams.get('issueIds')
    if (!ids) return []
    return ids.split(',').filter(Boolean)
  }, [searchParams])

  // Fetch all selected issues using useQueries
  const issueQueries = useQueries({
    queries: issueIds.map(id => ({
      queryKey: ['issues', 'detail', id],
      queryFn: () => getIssue(id),
      enabled: issueIds.length > 0,
    })),
  })
  
  const isLoading = issueQueries.some(q => q.isLoading)
  const hasError = issueQueries.some(q => q.error)

  // Combine all allocations into picking items
  const pickingItems = useMemo(() => {
    const items: PickingItem[] = []
    
    issueQueries.forEach((query) => {
      if (!query.data) return
      
      const issue = query.data
      // Only include Posted issues
      if (issue.status !== 'Posted') return
      
      issue.lines.forEach(line => {
        line.allocations.forEach(allocation => {
          items.push({
            allocationId: allocation.id,
            issueId: issue.id,
            issueExternalRef: issue.externalRef,
            lineNo: line.lineNo,
            productId: line.productId,
            variantId: line.variantId || null,
            stockItemId: allocation.stockItemId,
            qty: allocation.qty,
            sku: allocation.sku || null,
            lotNumber: allocation.lotNumber || null,
            expiryDate: allocation.expiryDate || null,
            shelfId: allocation.shelfId || null,
            shelfName: allocation.shelfName || null,
            warehouseId: allocation.warehouseId || null,
            warehouseName: allocation.warehouseName || null,
          })
        })
      })
    })
    
    return items
  }, [issueQueries])

  // Group by shelf and sort
  const itemsByShelf = useMemo(() => {
    const grouped = new Map<string, PickingItem[]>()
    
    pickingItems.forEach(item => {
      const shelfKey = item.shelfId || 'no-shelf'
      if (!grouped.has(shelfKey)) {
        grouped.set(shelfKey, [])
      }
      grouped.get(shelfKey)!.push(item)
    })
    
    // Sort items within each shelf by line number
    grouped.forEach(items => {
      items.sort((a, b) => {
        if (a.shelfName !== b.shelfName) {
          return (a.shelfName || '').localeCompare(b.shelfName || '', 'fa')
        }
        return a.lineNo - b.lineNo
      })
    })
    
    // Sort shelves by name
    const sortedShelves = Array.from(grouped.entries()).sort((a, b) => {
      const shelfA = pickingItems.find(i => (i.shelfId || 'no-shelf') === a[0])?.shelfName || ''
      const shelfB = pickingItems.find(i => (i.shelfId || 'no-shelf') === b[0])?.shelfName || ''
      return shelfA.localeCompare(shelfB, 'fa')
    })
    
    return new Map(sortedShelves)
  }, [pickingItems])

  // Track picked items
  const [pickedItems, setPickedItems] = useState<Set<string>>(new Set())

  function toggleItem(allocationId: string) {
    setPickedItems(prev => {
      const next = new Set(prev)
      if (next.has(allocationId)) {
        next.delete(allocationId)
      } else {
        next.add(allocationId)
      }
      return next
    })
  }

  function toggleShelf(shelfKey: string) {
    const shelfItems = itemsByShelf.get(shelfKey) || []
    const allPicked = shelfItems.every(item => pickedItems.has(item.allocationId))
    
    setPickedItems(prev => {
      const next = new Set(prev)
      if (allPicked) {
        shelfItems.forEach(item => next.delete(item.allocationId))
      } else {
        shelfItems.forEach(item => next.add(item.allocationId))
      }
      return next
    })
  }

  function toggleAll() {
    const allPicked = pickingItems.length > 0 && pickingItems.every(item => pickedItems.has(item.allocationId))
    
    setPickedItems(prev => {
      if (allPicked) {
        return new Set()
      } else {
        return new Set(pickingItems.map(item => item.allocationId))
      }
    })
  }

  async function handleConfirmPicking() {
    const allPicked = pickingItems.length > 0 && pickingItems.every(item => pickedItems.has(item.allocationId))
    
    if (!allPicked) {
      const unpickedCount = pickingItems.filter(item => !pickedItems.has(item.allocationId)).length
      const ok = await confirm.confirm({
        title: 'تایید جمع‌آوری',
        message: `${unpickedCount} کالا هنوز برداشت نشده است. آیا می‌خواهید ادامه دهید؟`,
      })
      if (!ok) return
    }

    // TODO: Call API to confirm picking
    toast.success('جمع‌آوری با موفقیت تایید شد')
    navigate('/issues')
  }

  const allPicked = pickingItems.length > 0 && pickingItems.every(item => pickedItems.has(item.allocationId))
  const pickedCount = pickedItems.size
  const totalCount = pickingItems.length

  if (issueIds.length === 0) {
    return (
      <div className="space-y-6">
        <PageHeader title="برنامه جمع‌آوری">هیچ رسیدی انتخاب نشده است</PageHeader>
        <div className="rounded-xl border border-slate-200 bg-white p-8 text-center shadow-sm">
          <p className="text-slate-600">لطفاً از لیست خروجی‌ها رسیدهای مورد نظر را انتخاب کنید</p>
          <button
            onClick={() => navigate('/issues')}
            className="mt-4 rounded-lg bg-emerald-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-emerald-700"
          >
            بازگشت به لیست خروجی‌ها
          </button>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="برنامه جمع‌آوری"
        actions={
          <div className="flex items-center gap-2">
            <button
              onClick={toggleAll}
              className="rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-50"
            >
              {allPicked ? 'لغو انتخاب همه' : 'انتخاب همه'}
            </button>
            <button
              onClick={handleConfirmPicking}
              disabled={pickedCount === 0}
              className="rounded-lg bg-emerald-600 px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-emerald-700 focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              تایید جمع‌آوری ({pickedCount}/{totalCount})
            </button>
          </div>
        }
      >
        برنامه جمع‌آوری کالاها بر اساس قفسه
      </PageHeader>

      {/* Stats */}
      <div className="grid gap-4 sm:grid-cols-3">
        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
          <div className="text-sm font-medium text-slate-500">کل کالاها</div>
          <div className="mt-2 text-3xl font-bold text-slate-900">{totalCount.toLocaleString('fa-IR')}</div>
        </div>
        <div className="rounded-xl border border-emerald-200 bg-emerald-50 p-4 shadow-sm">
          <div className="text-sm font-medium text-emerald-600">برداشت شده</div>
          <div className="mt-2 text-3xl font-bold text-emerald-700">{pickedCount.toLocaleString('fa-IR')}</div>
        </div>
        <div className="rounded-xl border border-slate-200 bg-slate-50 p-4 shadow-sm">
          <div className="text-sm font-medium text-slate-500">باقیمانده</div>
          <div className="mt-2 text-3xl font-bold text-slate-600">{(totalCount - pickedCount).toLocaleString('fa-IR')}</div>
        </div>
      </div>

      {/* Picking List */}
      <div className="rounded-xl border border-slate-200 bg-white shadow-sm overflow-hidden">
        {isLoading ? (
          <div className="flex items-center justify-center py-20">
            <Spinner className="h-8 w-8 text-emerald-600" />
          </div>
        ) : hasError ? (
          <div className="py-20 text-center">
            <div className="mx-auto mb-4 h-12 w-12 rounded-full bg-red-100 p-3 text-red-600">
              <svg fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126ZM12 15.75h.007v.008H12v-.008Z"
                />
              </svg>
            </div>
            <p className="text-slate-600">خطا در دریافت اطلاعات</p>
          </div>
        ) : pickingItems.length === 0 ? (
          <div className="py-20 text-center">
            <div className="mx-auto mb-4 h-12 w-12 rounded-full bg-slate-100 p-3 text-slate-400">
              <svg fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  d="M20.25 7.5l-.625 10.632a2.25 2.25 0 01-2.247 2.118H6.622a2.25 2.25 0 01-2.247-2.118L3.75 7.5M10 11.25h4M3.375 7.5h17.25c.621 0 1.125-.504 1.125-1.125v-1.5c0-.621-.504-1.125-1.125-1.125H3.375c-.621 0-1.125.504-1.125 1.125v1.5c0 .621.504 1.125 1.125 1.125z"
                />
              </svg>
            </div>
            <p className="text-slate-600">کالایی برای جمع‌آوری یافت نشد</p>
            <p className="mt-1 text-sm text-slate-400">رسیدهای انتخاب شده تخصیص‌یافته ندارند</p>
          </div>
        ) : (
          <div className="divide-y divide-slate-200">
            {Array.from(itemsByShelf.entries()).map(([shelfKey, items]) => {
              const shelfName = items[0]?.shelfName || 'بدون قفسه'
              const shelfPicked = items.every(item => pickedItems.has(item.allocationId))
              const shelfPickedCount = items.filter(item => pickedItems.has(item.allocationId)).length

              return (
                <div key={shelfKey} className="bg-white">
                  {/* Shelf Header */}
                  <div className={`border-l-4 border-b-2 px-6 py-4 shadow-sm ${
                    shelfPicked 
                      ? 'bg-emerald-50 border-emerald-500' 
                      : 'bg-blue-50 border-blue-400'
                  }`}>
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-3">
                        <input
                          type="checkbox"
                          checked={shelfPicked}
                          onChange={() => toggleShelf(shelfKey)}
                          className="h-5 w-5 rounded border-slate-300 text-emerald-600 focus:ring-emerald-500"
                        />
                        <div className="flex items-center gap-2">
                          <svg 
                            className={`h-5 w-5 ${
                              shelfPicked ? 'text-emerald-600' : 'text-blue-600'
                            }`}
                            fill="none" 
                            viewBox="0 0 24 24" 
                            strokeWidth={2} 
                            stroke="currentColor"
                          >
                            <path
                              strokeLinecap="round"
                              strokeLinejoin="round"
                              d="M20.25 7.5l-.625 10.632a2.25 2.25 0 01-2.247 2.118H6.622a2.25 2.25 0 01-2.247-2.118L3.75 7.5M10 11.25h4M3.375 7.5h17.25c.621 0 1.125-.504 1.125-1.125v-1.5c0-.621-.504-1.125-1.125-1.125H3.375c-.621 0-1.125.504-1.125 1.125v1.5c0 .621.504 1.125 1.125 1.125z"
                            />
                          </svg>
                          <div>
                            <h3 className={`font-bold text-base ${
                              shelfPicked ? 'text-emerald-900' : 'text-blue-900'
                            }`}>
                              {shelfName}
                            </h3>
                            <p className={`text-xs font-medium ${
                              shelfPicked ? 'text-emerald-700' : 'text-blue-700'
                            }`}>
                              {shelfPickedCount} از {items.length} کالا برداشت شده
                            </p>
                          </div>
                        </div>
                      </div>
                      <div className={`text-sm font-semibold px-3 py-1.5 rounded-lg ${
                        shelfPicked 
                          ? 'bg-emerald-100 text-emerald-800' 
                          : 'bg-blue-100 text-blue-800'
                      }`}>
                        {items.length} کالا
                      </div>
                    </div>
                  </div>

                  {/* Items List */}
                  <div className="divide-y divide-slate-100">
                    {items.map((item) => {
                      const isPicked = pickedItems.has(item.allocationId)
                      
                      return (
                        <div
                          key={item.allocationId}
                          className={`px-6 py-4 transition-colors ${
                            isPicked ? 'bg-emerald-50/50' : 'hover:bg-slate-50'
                          }`}
                        >
                          <div className="flex items-center gap-4">
                            <input
                              type="checkbox"
                              checked={isPicked}
                              onChange={() => toggleItem(item.allocationId)}
                              className="h-5 w-5 rounded border-slate-300 text-emerald-600 focus:ring-emerald-500"
                            />
                            <div className="flex-1 grid grid-cols-12 gap-4">
                              <div className="col-span-3">
                                <div className="text-sm font-medium text-slate-900">
                                  {item.sku || 'نامشخص'}
                                </div>
                                <div className="mt-0.5 text-xs text-slate-500">
                                  رسید: {item.issueExternalRef || item.issueId.substring(0, 8)} - خط {item.lineNo}
                                </div>
                              </div>
                              <div className="col-span-2">
                                <div className="text-xs text-slate-500">تعداد</div>
                                <div className="mt-0.5 text-sm font-medium text-slate-900">
                                  {item.qty.toLocaleString('fa-IR')}
                                </div>
                              </div>
                              <div className="col-span-2">
                                <div className="text-xs text-slate-500">شماره لات</div>
                                <div className="mt-0.5 text-sm text-slate-700">
                                  {item.lotNumber || '-'}
                                </div>
                              </div>
                              <div className="col-span-2">
                                <div className="text-xs text-slate-500">تاریخ انقضا</div>
                                <div className="mt-0.5 text-sm text-slate-700">
                                  {item.expiryDate
                                    ? new Date(item.expiryDate).toLocaleDateString('fa-IR')
                                    : '-'}
                                </div>
                              </div>
                              <div className="col-span-3">
                                <div className="text-xs text-slate-500">انبار</div>
                                <div className="mt-0.5 text-sm text-slate-700">
                                  {item.warehouseName || '-'}
                                </div>
                              </div>
                            </div>
                            {isPicked && (
                              <div className="flex-shrink-0">
                                <svg
                                  className="h-5 w-5 text-emerald-600"
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
                              </div>
                            )}
                          </div>
                        </div>
                      )
                    })}
                  </div>
                </div>
              )
            })}
          </div>
        )}
      </div>
    </div>
  )
}

