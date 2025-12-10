import { useState, useMemo } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { PageHeader } from '@/shared/components/PageHeader'
import { Spinner } from '@/shared/components/Spinner'
import { useIssuesList, useCreateIssue } from '../queries'
import { CreateIssueModal } from '../components/CreateIssueModal'
import {
  IssueStatusLabels,
  IssueStatusColors,
  type IssuesListFilters,
} from '../types'
import { useWarehouseNames } from '@/shared/hooks/useWarehouses'

export function IssuesListPage() {
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()
  const createIssue = useCreateIssue()
  const [showCreateModal, setShowCreateModal] = useState(false)
  const [creating, setCreating] = useState(false)
  const { getWarehouseName } = useWarehouseNames()

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

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <Spinner />
      </div>
    )
  }

  if (error) {
    return (
      <div className="card p-4">
        <p className="text-red-600">خطا در دریافت لیست خروجی‌ها: {error instanceof Error ? error.message : 'خطای ناشناخته'}</p>
      </div>
    )
  }

  const issues = data?.items || []
  const totalPages = data?.totalPages || 0
  const currentPage = data?.page || 1

  return (
    <div className="space-y-4">
      <PageHeader
        title="خروجی‌ها"
        actions={
          <button
            onClick={() => setShowCreateModal(true)}
            className="btn-primary"
          >
            ایجاد خروجی جدید
          </button>
        }
      >
        مدیریت خروجی‌های انبار
      </PageHeader>

      {/* Filters */}
      <div className="card p-4">
        <form onSubmit={handleSearch} className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div>
              <label className="block text-sm font-medium mb-1">جستجو</label>
              <div className="flex gap-2">
                <input
                  type="text"
                  value={searchInput}
                  onChange={(e) => setSearchInput(e.target.value)}
                  placeholder="شناسه یا مرجع خارجی"
                  className="flex-1 input"
                />
                <button type="submit" className="btn-secondary">
                  جستجو
                </button>
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium mb-1">وضعیت</label>
              <select
                value={statusFilter}
                onChange={(e) => handleStatusChange(e.target.value)}
                className="w-full input"
              >
                <option value="">همه</option>
                <option value="1">پیش‌نویس</option>
                <option value="2">ثبت شده</option>
                <option value="3">لغو شده</option>
              </select>
            </div>

            <div className="flex items-end">
              <button
                type="button"
                onClick={clearFilters}
                className="btn-secondary w-full"
              >
                پاک کردن فیلترها
              </button>
            </div>
          </div>
        </form>
      </div>

      {/* Table */}
      <div className="card p-4">
        {issues.length === 0 ? (
          <p className="text-center text-gray-600 py-8">هیچ خروجی‌ای یافت نشد</p>
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b">
                    <th className="text-right p-2">شناسه</th>
                    <th className="text-right p-2">انبار</th>
                    <th className="text-right p-2">وضعیت</th>
                    <th className="text-right p-2">مرجع</th>
                    <th className="text-right p-2">تاریخ سند</th>
                    <th className="text-right p-2">تاریخ ثبت</th>
                    <th className="text-right p-2">تعداد خطوط</th>
                    <th className="text-right p-2">درخواستی</th>
                    <th className="text-right p-2">تخصیص یافته</th>
                    <th className="text-right p-2">باقیمانده</th>
                    <th className="text-right p-2">عملیات</th>
                  </tr>
                </thead>
                <tbody>
                  {issues.map((issue) => (
                    <tr key={issue.id} className="border-b hover:bg-gray-50">
                      <td className="p-2 font-mono text-xs">{issue.id.slice(0, 8)}...</td>
                      <td className="p-2">
                        {issue.warehouseId 
                          ? (issue.warehouseName || getWarehouseName(issue.warehouseId))
                          : 'انتخاب نشده'}
                      </td>
                      <td className="p-2">
                        <span className={`badge ${IssueStatusColors[issue.status] || 'bg-gray-100 text-gray-800'}`}>
                          {IssueStatusLabels[issue.status] || issue.status}
                        </span>
                      </td>
                      <td className="p-2">{issue.externalRef || '-'}</td>
                      <td className="p-2">{new Date(issue.docDate).toLocaleDateString('fa-IR')}</td>
                      <td className="p-2">
                        {issue.postedAt ? new Date(issue.postedAt).toLocaleDateString('fa-IR') : '-'}
                      </td>
                      <td className="p-2">{issue.linesCount}</td>
                      <td className="p-2">{issue.totalRequestedQty.toLocaleString('fa-IR')}</td>
                      <td className="p-2">{issue.totalAllocatedQty.toLocaleString('fa-IR')}</td>
                      <td className="p-2">
                        <span className={issue.totalRemainingQty > 0 ? 'text-orange-600' : 'text-green-600'}>
                          {issue.totalRemainingQty.toLocaleString('fa-IR')}
                        </span>
                      </td>
                      <td className="p-2">
                        <button
                          onClick={() => navigate(`/issues/${issue.id}`)}
                          className="btn-secondary text-xs px-2 py-1"
                        >
                          مشاهده
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* Pagination */}
            {totalPages > 1 && (
              <div className="flex items-center justify-between mt-4 pt-4 border-t">
                <div className="text-sm text-gray-600">
                  صفحه {currentPage} از {totalPages} • مجموع {data?.totalCount || 0} مورد
                </div>
                <div className="flex gap-2">
                  <button
                    onClick={() => handlePageChange(currentPage - 1)}
                    disabled={currentPage <= 1}
                    className="btn-secondary disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    قبلی
                  </button>
                  <button
                    onClick={() => handlePageChange(currentPage + 1)}
                    disabled={currentPage >= totalPages}
                    className="btn-secondary disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    بعدی
                  </button>
                </div>
              </div>
            )}
          </>
        )}
      </div>

      {/* Create Modal */}
      <CreateIssueModal
        isOpen={showCreateModal}
        onClose={() => setShowCreateModal(false)}
        onSubmit={handleCreateIssue}
        isSubmitting={creating}
      />
    </div>
  )
}
