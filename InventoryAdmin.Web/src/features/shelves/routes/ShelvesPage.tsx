import { useState } from 'react'
import { PageHeader } from '@/shared/components/PageHeader'
import { Spinner } from '@/shared/components/Spinner'
import {
  useAllShelves,
  useCreateShelf,
  useCreateShelvesBatch,
  useUpdateShelf,
  useActivateShelf,
  useDeactivateShelf,
} from '../queries'
import { useActiveWarehouses, useWarehouseNames } from '@/shared/hooks/useWarehouses'
import { useConfirm } from '@/shared/components/confirm/ConfirmProvider'
import { CreateShelfModal } from '../components/CreateShelfModal'
import { CreateShelvesBatchModal } from '../components/CreateShelvesBatchModal'
import { EditShelfModal } from '../components/EditShelfModal'
import type { Shelf } from '../api'

export function ShelvesPage() {
  const { data: shelves, isLoading, error } = useAllShelves()
  const { data: warehouses } = useActiveWarehouses()
  const { getWarehouseName } = useWarehouseNames()
  const createShelf = useCreateShelf()
  const createShelvesBatch = useCreateShelvesBatch()
  const updateShelf = useUpdateShelf()
  const activateShelf = useActivateShelf()
  const deactivateShelf = useDeactivateShelf()
  const confirm = useConfirm()

  const [showCreateModal, setShowCreateModal] = useState(false)
  const [showBatchModal, setShowBatchModal] = useState(false)
  const [editingShelf, setEditingShelf] = useState<Shelf | null>(null)
  const [warehouseFilter, setWarehouseFilter] = useState<string>('')

  async function handleCreate(data: { warehouseId: string; name: string; description?: string }) {
    await createShelf.mutateAsync(data)
    setShowCreateModal(false)
  }

  async function handleCreateBatch(data: {
    warehouseId: string
    rows: number
    columns: number
    levels?: number
    prefix?: string
    description?: string
  }) {
    await createShelvesBatch.mutateAsync(data)
    setShowBatchModal(false)
  }

  async function handleUpdate(data: { name: string; description?: string }) {
    if (!editingShelf) return
    await updateShelf.mutateAsync({ id: editingShelf.id, dto: data })
    setEditingShelf(null)
  }

  async function handleActivate(shelf: Shelf) {
    const ok = await confirm.confirm({
      title: 'فعال کردن قفسه',
      message: `آیا می‌خواهید قفسه "${shelf.name}" را فعال کنید؟`,
    })
    if (ok) {
      await activateShelf.mutateAsync(shelf.id)
    }
  }

  async function handleDeactivate(shelf: Shelf) {
    const ok = await confirm.confirm({
      title: 'غیرفعال کردن قفسه',
      message: `آیا می‌خواهید قفسه "${shelf.name}" را غیرفعال کنید؟`,
    })
    if (ok) {
      await deactivateShelf.mutateAsync(shelf.id)
    }
  }

  const filteredShelves = warehouseFilter
    ? shelves?.filter((s) => s.warehouseId === warehouseFilter)
    : shelves

  const activeCount = filteredShelves?.filter((s) => s.isActive).length ?? 0
  const inactiveCount = filteredShelves?.filter((s) => !s.isActive).length ?? 0

  return (
    <div className="space-y-6">
      <PageHeader
        title="قفسه‌ها"
        actions={
          <button
            onClick={() => setShowBatchModal(true)}
            className="inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-emerald-700 focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:ring-offset-2"
          >
            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
            </svg>
            ایجاد گروهی قفسه
          </button>
        }
      >
        مدیریت قفسه‌های انبار
      </PageHeader>

      {/* Stats Cards */}
      <div className="grid gap-4 sm:grid-cols-3">
        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
          <div className="text-sm font-medium text-slate-500">کل قفسه‌ها</div>
          <div className="mt-2 text-3xl font-bold text-slate-900">{filteredShelves?.length ?? 0}</div>
        </div>
        <div className="rounded-xl border border-emerald-200 bg-emerald-50 p-4 shadow-sm">
          <div className="text-sm font-medium text-emerald-600">قفسه‌های فعال</div>
          <div className="mt-2 text-3xl font-bold text-emerald-700">{activeCount}</div>
        </div>
        <div className="rounded-xl border border-slate-200 bg-slate-50 p-4 shadow-sm">
          <div className="text-sm font-medium text-slate-500">قفسه‌های غیرفعال</div>
          <div className="mt-2 text-3xl font-bold text-slate-600">{inactiveCount}</div>
        </div>
      </div>

      {/* Filter */}
      <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
        <div className="flex items-end gap-4">
          <div className="min-w-[200px]">
            <label className="mb-2 block text-sm font-medium text-slate-700">فیلتر بر اساس انبار</label>
            <select
              value={warehouseFilter}
              onChange={(e) => setWarehouseFilter(e.target.value)}
              className="w-full rounded-lg border border-slate-300 py-2 px-3 text-sm focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500"
            >
              <option value="">همه انبارها</option>
              {warehouses?.map((w) => (
                <option key={w.id} value={w.id}>
                  {w.name}
                </option>
              ))}
            </select>
          </div>
          {warehouseFilter && (
            <button
              onClick={() => setWarehouseFilter('')}
              className="rounded-lg px-4 py-2 text-sm font-medium text-slate-500 transition-colors hover:bg-slate-100"
            >
              پاک کردن فیلتر
            </button>
          )}
        </div>
      </div>

      {/* Shelves List */}
      <div className="rounded-xl border border-slate-200 bg-white shadow-sm overflow-hidden">
        {isLoading ? (
          <div className="flex items-center justify-center py-20">
            <Spinner className="h-8 w-8 text-emerald-600" />
          </div>
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
        ) : !filteredShelves || filteredShelves.length === 0 ? (
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
            <p className="text-slate-600">قفسه‌ای یافت نشد</p>
            <p className="mt-1 text-sm text-slate-400">
              {warehouseFilter ? 'در این انبار قفسه‌ای وجود ندارد' : 'برای شروع یک قفسه جدید ایجاد کنید'}
            </p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-slate-200 bg-slate-50 text-right">
                  <th className="whitespace-nowrap px-6 py-4 font-semibold text-slate-600">نام قفسه</th>
                  <th className="whitespace-nowrap px-6 py-4 font-semibold text-slate-600">انبار</th>
                  <th className="whitespace-nowrap px-6 py-4 font-semibold text-slate-600">توضیحات</th>
                  <th className="whitespace-nowrap px-6 py-4 font-semibold text-slate-600">وضعیت</th>
                  <th className="whitespace-nowrap px-6 py-4 font-semibold text-slate-600">عملیات</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {filteredShelves.map((shelf) => (
                  <tr key={shelf.id} className="transition-colors hover:bg-slate-50">
                    <td className="whitespace-nowrap px-6 py-4">
                      <div className="flex items-center gap-3">
                        <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-emerald-100">
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
                              d="M20.25 7.5l-.625 10.632a2.25 2.25 0 01-2.247 2.118H6.622a2.25 2.25 0 01-2.247-2.118L3.75 7.5M10 11.25h4M3.375 7.5h17.25c.621 0 1.125-.504 1.125-1.125v-1.5c0-.621-.504-1.125-1.125-1.125H3.375c-.621 0-1.125.504-1.125 1.125v1.5c0 .621.504 1.125 1.125 1.125z"
                            />
                          </svg>
                        </div>
                        <span className="font-medium text-slate-900">{shelf.name}</span>
                      </div>
                    </td>
                    <td className="whitespace-nowrap px-6 py-4 text-slate-700">
                      {shelf.warehouseName || getWarehouseName(shelf.warehouseId)}
                    </td>
                    <td className="px-6 py-4 text-slate-600">{shelf.description || '-'}</td>
                    <td className="whitespace-nowrap px-6 py-4">
                      <span
                        className={`inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-xs font-medium ${
                          shelf.isActive
                            ? 'bg-emerald-100 text-emerald-700'
                            : 'bg-slate-100 text-slate-600'
                        }`}
                      >
                        <span className={`h-1.5 w-1.5 rounded-full ${shelf.isActive ? 'bg-emerald-500' : 'bg-slate-400'}`} />
                        {shelf.isActive ? 'فعال' : 'غیرفعال'}
                      </span>
                    </td>
                    <td className="whitespace-nowrap px-6 py-4">
                      <div className="flex items-center gap-1">
                        {shelf.isActive ? (
                          <>
                            <button
                              onClick={() => setEditingShelf(shelf)}
                              className="rounded-lg p-2 text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-600"
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
                              onClick={() => handleDeactivate(shelf)}
                              disabled={deactivateShelf.isPending}
                              className="rounded-lg p-2 text-orange-500 transition-colors hover:bg-orange-50 hover:text-orange-700 disabled:opacity-50"
                              title="غیرفعال کردن"
                            >
                              <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
                                <path
                                  strokeLinecap="round"
                                  strokeLinejoin="round"
                                  d="M18.364 18.364A9 9 0 0 0 5.636 5.636m12.728 12.728A9 9 0 0 1 5.636 5.636m12.728 12.728L5.636 5.636"
                                />
                              </svg>
                            </button>
                          </>
                        ) : (
                          <button
                            onClick={() => handleActivate(shelf)}
                            disabled={activateShelf.isPending}
                            className="rounded-lg p-2 text-emerald-500 transition-colors hover:bg-emerald-50 hover:text-emerald-700 disabled:opacity-50"
                            title="فعال کردن"
                          >
                            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
                              <path
                                strokeLinecap="round"
                                strokeLinejoin="round"
                                d="M9 12.75 11.25 15 15 9.75M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"
                              />
                            </svg>
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Create Batch Modal */}
      <CreateShelvesBatchModal
        isOpen={showBatchModal}
        onClose={() => setShowBatchModal(false)}
        onSubmit={handleCreateBatch}
        isSubmitting={createShelvesBatch.isPending}
      />

      {/* Create Modal */}
      <CreateShelfModal
        isOpen={showCreateModal}
        onClose={() => setShowCreateModal(false)}
        onSubmit={handleCreate}
        isSubmitting={createShelf.isPending}
      />

      {/* Edit Modal */}
      <EditShelfModal
        isOpen={!!editingShelf}
        shelf={editingShelf}
        onClose={() => setEditingShelf(null)}
        onSubmit={handleUpdate}
        isSubmitting={updateShelf.isPending}
      />
    </div>
  )
}

