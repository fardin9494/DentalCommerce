import { useState, useMemo } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { PageHeader } from '@/shared/components/PageHeader'
import { TableSkeleton } from '@/shared/components/TableSkeleton'
import { useIssuesList, useCreateIssue } from '../queries'
import { CreateIssueModal } from '../components/CreateIssueModal'
import {
  IssueStatusLabels,
  IssueStatusColors,
  type IssuesListFilters,
} from '../types'
import { useWarehouseNames } from '@/shared/hooks/useWarehouses'
import { useSortableTable } from '@/shared/hooks/useSortableTable'
import { SortableHeader } from '@/shared/components/SortableHeader'
import { issueDisplayRef } from '@/shared/utils/inventoryDocumentReference'
import { PermissionGate } from '@/app/permissions'
import { InventoryPermissionKeys } from '@/app/inventoryPermissionKeys'

// Type for issue list item
interface IssueListItem {
  id: string
  warehouseId?: string | null
  warehouseName?: string | null
  status: string
  externalRef?: string | null
  docDate: string
  postedAt?: string | null
  linesCount: number
  totalRequestedQty: number
  totalAllocatedQty: number
  totalRemainingQty: number
}

export function IssuesListPage() {
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()
  const createIssue = useCreateIssue()
  const [showCreateModal, setShowCreateModal] = useState(false)
  const [creating, setCreating] = useState(false)
  const { getWarehouseName } = useWarehouseNames()
  const [selectedIssues, setSelectedIssues] = useState<Set<string>>(new Set())

  // Get filters from URL
  const filters: IssuesListFilters = useMemo(() => ({
    page: parseInt(searchParams.get('page') || '1', 10),
    pageSize: parseInt(searchParams.get('pageSize') || '20', 10),
    status: searchParams.get('status') ? parseInt(searchParams.get('status')!, 10) : undefined,
    search: searchParams.get('search') || undefined,
    warehouseId: searchParams.get('warehouseId') || undefined,
    fromDate: searchParams.get('fromDate') || undefined,
    toDate: searchParams.get('toDate') || undefined,
  }), [searchParams])

  const { data, isLoading, error } = useIssuesList(filters)

  // Sorting - default by docDate descending (newest first)
  const { sortedData, requestSort, getSortIndicator, sortConfig } = useSortableTable<IssueListItem>({
    data: (data?.items || []) as IssueListItem[],
    defaultSortKey: 'docDate',
    defaultDirection: 'desc',
  })

  // Local filter states for UI
  const [searchInput, setSearchInput] = useState(filters.search || '')
  const [statusFilter, setStatusFilter] = useState<string>(filters.status?.toString() || '')

  function updateFilters(newFilters: Partial<IssuesListFilters>) {
    const params = new URLSearchParams(searchParams)

    Object.entries(newFilters).forEach(([key, value]) => {
      if (value === undefined || value === '' || value === null) {
        params.delete(key)
      } else {
        params.set(key, String(value))
      }
    })

    // Reset to page 1 when filters change (except when changing page)
    if (!('page' in newFilters)) {
      params.set('page', '1')
    }

    setSearchParams(params)
  }

  function handleSearch(e: React.FormEvent) {
    e.preventDefault()
    updateFilters({ search: searchInput || undefined })
  }

  function handleStatusChange(value: string) {
    setStatusFilter(value)
    updateFilters({ status: value ? parseInt(value, 10) : undefined })
  }

  function handlePageChange(newPage: number) {
    updateFilters({ page: newPage })
  }

  function clearFilters() {
    setSearchInput('')
    setStatusFilter('')
    setSearchParams(new URLSearchParams())
  }

  async function handleCreateIssue(data: { warehouseId?: string; externalRef?: string }) {
    setCreating(true)
    try {
      const result = await createIssue.mutateAsync({
        warehouseId: data.warehouseId || null,
        externalRef: data.externalRef || null,
        docDateUtc: new Date().toISOString(),
      })
      setShowCreateModal(false)
      navigate(`/issues/${result.id}`)
    } catch {
      // Error handled by mutation
    } finally {
      setCreating(false)
    }
  }

  function formatDate(dateStr: string) {
    try {
      return new Date(dateStr).toLocaleDateString('fa-IR', {
        year: 'numeric',
        month: 'short',
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

  const hasActiveFilters = statusFilter || searchInput

  // Filter selected issues to only Posted ones
  const selectedPostedIssues = useMemo(() => {
    if (!data) return []
    return data.items
      .filter(issue => issue.status === 'Posted' && selectedIssues.has(issue.id))
      .map(issue => issue.id)
  }, [data, selectedIssues])

  function toggleIssueSelection(issueId: string) {
    setSelectedIssues(prev => {
      const next = new Set(prev)
      if (next.has(issueId)) {
        next.delete(issueId)
      } else {
        next.add(issueId)
      }
      return next
    })
  }

  function toggleAllSelection() {
    if (!data) return
    const allSelected = data.items.every(issue => selectedIssues.has(issue.id))

    setSelectedIssues(prev => {
      if (allSelected) {
        return new Set()
      } else {
        return new Set(data.items.map(issue => issue.id))
      }
    })
  }

  function handleGoToPickingPlan() {
    if (selectedPostedIssues.length === 0) return
    navigate(`/issues/picking-plan?issueIds=${selectedPostedIssues.join(',')}`)
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="خروجی‌ها"
        actions={
          <div className="flex items-center gap-2">
            <PermissionGate permission={InventoryPermissionKeys.IssuesAllocate}>
              {selectedPostedIssues.length > 0 && (
              <button
                onClick={handleGoToPickingPlan}
                className="inline-flex items-center gap-2 rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
              >
                <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M20.25 7.5l-.625 10.632a2.25 2.25 0 01-2.247 2.118H6.622a2.25 2.25 0 01-2.247-2.118L3.75 7.5M10 11.25h4M3.375 7.5h17.25c.621 0 1.125-.504 1.125-1.125v-1.5c0-.621-.504-1.125-1.125-1.125H3.375c-.621 0-1.125.504-1.125 1.125v1.5c0 .621.504 1.125 1.125 1.125z" />
                </svg>
                برنامه جمع‌آوری ({selectedPostedIssues.length})
              </button>
            )}
            </PermissionGate>
            <PermissionGate permission={InventoryPermissionKeys.IssuesCreate}>
              <button
              onClick={() => setShowCreateModal(true)}
              className="inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-emerald-700 focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:ring-offset-2"
            >
              <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
              </svg>
              خروجی جدید
            </button>
            </PermissionGate>
          </div>
        }
      >
        مدیریت خروجی‌های انبار
      </PageHeader>

      {/* Create Issue Modal */}
      <CreateIssueModal
        isOpen={showCreateModal}
        onClose={() => setShowCreateModal(false)}
        onSubmit={handleCreateIssue}
        isSubmitting={creating}
      />

      {/* Filters */}
      <div className="rounded-xl border border-slate-200 bg-white p-5 shadow-md">
        <form onSubmit={handleSearch} className="flex flex-wrap items-end gap-4">
          {/* Search */}
          <div className="flex-1 min-w-[200px]">
            <label className="mb-1.5 block text-sm font-medium text-slate-700">جستجو</label>
            <div className="relative">
              <input
                type="text"
                value={searchInput}
                onChange={(e) => setSearchInput(e.target.value)}
                placeholder="شماره مرجع یا شناسه..."
                className="w-full rounded-lg border border-slate-300 py-2 pl-10 pr-4 text-sm placeholder-slate-400 focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500"
              />
              <svg
                className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400"
                fill="none"
                viewBox="0 0 24 24"
                strokeWidth={2}
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  d="m21 21-5.197-5.197m0 0A7.5 7.5 0 1 0 5.196 5.196a7.5 7.5 0 0 0 10.607 10.607Z"
                />
              </svg>
            </div>
          </div>

          {/* Status Filter */}
          <div className="min-w-[150px]">
            <label className="mb-1.5 block text-sm font-medium text-slate-700">وضعیت</label>
            <select
              value={statusFilter}
              onChange={(e) => handleStatusChange(e.target.value)}
              className="w-full rounded-lg border border-slate-300 py-2 px-3 text-sm focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20"
            >
              <option value="">همه</option>
              <option value="1">پیش‌نویس</option>
              <option value="2">ثبت شده</option>
              <option value="3">لغو شده</option>
            </select>
          </div>

          {/* Buttons */}
          <div className="flex gap-2">
            <button
              type="submit"
              className="rounded-lg bg-emerald-600 px-4 py-2 text-sm font-medium text-white shadow-sm transition-colors hover:bg-emerald-700"
            >
              اعمال
            </button>
            {hasActiveFilters && (
              <button
                type="button"
                onClick={clearFilters}
                className="rounded-lg px-4 py-2 text-sm font-medium text-slate-600 transition-colors hover:bg-slate-100 border border-slate-200"
              >
                پاک کردن
              </button>
            )}
          </div>
        </form>
      </div>

      {/* Results */}
      <div className="rounded-xl border border-slate-200 bg-white shadow-sm overflow-hidden">
        {isLoading ? (
          <TableSkeleton columns={12} rows={10} />
        ) : error ? (
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
            <p className="mt-1 text-sm text-slate-400">{(error as Error).message}</p>
          </div>
        ) : !data || data.items.length === 0 ? (
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
            <p className="text-slate-600">خروجی‌ای یافت نشد</p>
            <p className="mt-1 text-sm text-slate-400">
              {hasActiveFilters ? 'فیلترهای خود را تغییر دهید' : 'برای شروع یک خروجی جدید ایجاد کنید'}
            </p>
          </div>
        ) : (
          <>
            {/* Table */}
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-slate-200 bg-slate-50 text-right">
                    <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">
                      <input
                        type="checkbox"
                        checked={data.items.length > 0 && data.items.every(issue => selectedIssues.has(issue.id))}
                        onChange={toggleAllSelection}
                        className="h-4 w-4 rounded border-slate-300 text-emerald-600 focus:ring-emerald-500"
                      />
                    </th>
                    <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">شناسه</th>
                    <SortableHeader
                      label="انبار"
                      sortKey="warehouseId"
                      currentSortKey={sortConfig.key as string}
                      currentDirection={getSortIndicator('warehouseId' as keyof IssueListItem)}
                      onSort={(key) => requestSort(key as keyof IssueListItem)}
                    />
                    <SortableHeader
                      label="وضعیت"
                      sortKey="status"
                      currentSortKey={sortConfig.key as string}
                      currentDirection={getSortIndicator('status' as keyof IssueListItem)}
                      onSort={(key) => requestSort(key as keyof IssueListItem)}
                    />
                    <SortableHeader
                      label="تاریخ سند"
                      sortKey="docDate"
                      currentSortKey={sortConfig.key as string}
                      currentDirection={getSortIndicator('docDate' as keyof IssueListItem)}
                      onSort={(key) => requestSort(key as keyof IssueListItem)}
                    />
                    <SortableHeader
                      label="تاریخ ثبت"
                      sortKey="postedAt"
                      currentSortKey={sortConfig.key as string}
                      currentDirection={getSortIndicator('postedAt' as keyof IssueListItem)}
                      onSort={(key) => requestSort(key as keyof IssueListItem)}
                    />
                    <SortableHeader
                      label="تعداد خطوط"
                      sortKey="linesCount"
                      currentSortKey={sortConfig.key as string}
                      currentDirection={getSortIndicator('linesCount' as keyof IssueListItem)}
                      onSort={(key) => requestSort(key as keyof IssueListItem)}
                    />
                    <SortableHeader
                      label="درخواستی"
                      sortKey="totalRequestedQty"
                      currentSortKey={sortConfig.key as string}
                      currentDirection={getSortIndicator('totalRequestedQty' as keyof IssueListItem)}
                      onSort={(key) => requestSort(key as keyof IssueListItem)}
                    />
                    <SortableHeader
                      label="تخصیص یافته"
                      sortKey="totalAllocatedQty"
                      currentSortKey={sortConfig.key as string}
                      currentDirection={getSortIndicator('totalAllocatedQty' as keyof IssueListItem)}
                      onSort={(key) => requestSort(key as keyof IssueListItem)}
                    />
                    <SortableHeader
                      label="باقیمانده"
                      sortKey="totalRemainingQty"
                      currentSortKey={sortConfig.key as string}
                      currentDirection={getSortIndicator('totalRemainingQty' as keyof IssueListItem)}
                      onSort={(key) => requestSort(key as keyof IssueListItem)}
                    />
                    <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">عملیات</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {sortedData.map((issue) => {
                    const isSelected = selectedIssues.has(issue.id)
                    const isPosted = issue.status === 'Posted'

                    return (
                      <tr
                        key={issue.id}
                        className={`transition-colors hover:bg-slate-50 even:bg-slate-50/60 ${isSelected ? 'bg-blue-50' : ''
                          }`}
                      >
                        <td className="whitespace-nowrap px-4 py-3">
                          <input
                            type="checkbox"
                            checked={isSelected}
                            onChange={(e) => {
                              e.stopPropagation()
                              toggleIssueSelection(issue.id)
                            }}
                            className="h-4 w-4 rounded border-slate-300 text-emerald-600 focus:ring-emerald-500"
                          />
                        </td>
                        <td
                          className="whitespace-nowrap px-4 py-3 cursor-pointer"
                          onClick={() => navigate(`/issues/${issue.id}`)}
                        >
                          <span className="font-mono text-xs text-slate-500">
                            {issueDisplayRef(issue.id, issue.externalRef)}
                          </span>
                        </td>
                        <td
                          className="whitespace-nowrap px-4 py-3 text-slate-700 cursor-pointer"
                          onClick={() => navigate(`/issues/${issue.id}`)}
                        >
                          {issue.warehouseId
                            ? (issue.warehouseName || getWarehouseName(issue.warehouseId))
                            : 'انتخاب نشده'}
                        </td>
                        <td
                          className="whitespace-nowrap px-4 py-3 cursor-pointer"
                          onClick={() => navigate(`/issues/${issue.id}`)}
                        >
                          <span
                            className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${IssueStatusColors[issue.status] || 'bg-slate-100 text-slate-700'
                              }`}
                          >
                            {IssueStatusLabels[issue.status] || issue.status}
                          </span>
                        </td>
                        <td
                          className="whitespace-nowrap px-4 py-3 text-slate-600 cursor-pointer"
                          onClick={() => navigate(`/issues/${issue.id}`)}
                        >
                          {formatDate(issue.docDate)}
                        </td>
                        <td
                          className="whitespace-nowrap px-4 py-3 text-slate-500 text-xs cursor-pointer"
                          onClick={() => navigate(`/issues/${issue.id}`)}
                        >
                          {formatDateTime(issue.postedAt)}
                        </td>
                        <td
                          className="whitespace-nowrap px-4 py-3 text-center text-slate-700 cursor-pointer"
                          onClick={() => navigate(`/issues/${issue.id}`)}
                        >
                          {issue.linesCount}
                        </td>
                        <td
                          className="whitespace-nowrap px-4 py-3 text-center text-slate-700 cursor-pointer"
                          onClick={() => navigate(`/issues/${issue.id}`)}
                        >
                          {issue.totalRequestedQty.toLocaleString('fa-IR')}
                        </td>
                        <td
                          className="whitespace-nowrap px-4 py-3 text-center text-slate-700 cursor-pointer"
                          onClick={() => navigate(`/issues/${issue.id}`)}
                        >
                          {issue.totalAllocatedQty.toLocaleString('fa-IR')}
                        </td>
                        <td
                          className="whitespace-nowrap px-4 py-3 text-center cursor-pointer"
                          onClick={() => navigate(`/issues/${issue.id}`)}
                        >
                          <span
                            className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${issue.totalRemainingQty > 0
                                ? 'bg-orange-100 text-orange-700'
                                : 'bg-green-100 text-green-700'
                              }`}
                          >
                            {issue.totalRemainingQty.toLocaleString('fa-IR')}
                          </span>
                        </td>
                        <td className="whitespace-nowrap px-4 py-3">
                          <button
                            onClick={(e) => {
                              e.stopPropagation()
                              navigate(`/issues/${issue.id}`)
                            }}
                            className="rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-600"
                          >
                            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
                              <path strokeLinecap="round" strokeLinejoin="round" d="M2.036 12.322a1.012 1.012 0 0 1 0-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178Z" />
                              <path strokeLinecap="round" strokeLinejoin="round" d="M15 12a3 3 0 1 1-6 0 3 3 0 0 1 6 0Z" />
                            </svg>
                          </button>
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>

            {/* Pagination */}
            {data.totalPages > 1 && (
              <div className="flex items-center justify-between border-t border-slate-200 bg-slate-50 px-4 py-3">
                <div className="text-sm text-slate-600">
                  نمایش {((data.page - 1) * data.pageSize) + 1} تا{' '}
                  {Math.min(data.page * data.pageSize, data.totalCount)} از {data.totalCount.toLocaleString('fa-IR')} خروجی
                </div>
                <div className="flex items-center gap-1">
                  <button
                    onClick={() => handlePageChange(data.page - 1)}
                    disabled={data.page <= 1}
                    className="rounded-lg p-2 text-slate-600 transition-colors hover:bg-slate-200 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" d="m8.25 4.5 7.5 7.5-7.5 7.5" />
                    </svg>
                  </button>

                  {/* Page numbers */}
                  {Array.from({ length: Math.min(5, data.totalPages) }, (_, i) => {
                    let pageNum: number
                    if (data.totalPages <= 5) {
                      pageNum = i + 1
                    } else if (data.page <= 3) {
                      pageNum = i + 1
                    } else if (data.page >= data.totalPages - 2) {
                      pageNum = data.totalPages - 4 + i
                    } else {
                      pageNum = data.page - 2 + i
                    }
                    return (
                      <button
                        key={pageNum}
                        onClick={() => handlePageChange(pageNum)}
                        className={`min-w-[36px] rounded-lg px-3 py-1.5 text-sm font-medium transition-colors ${data.page === pageNum
                            ? 'bg-emerald-600 text-white'
                            : 'text-slate-600 hover:bg-slate-200'
                          }`}
                      >
                        {pageNum.toLocaleString('fa-IR')}
                      </button>
                    )
                  })}

                  <button
                    onClick={() => handlePageChange(data.page + 1)}
                    disabled={data.page >= data.totalPages}
                    className="rounded-lg p-2 text-slate-600 transition-colors hover:bg-slate-200 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 19.5 8.25 12l7.5-7.5" />
                    </svg>
                  </button>
                </div>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  )
}

