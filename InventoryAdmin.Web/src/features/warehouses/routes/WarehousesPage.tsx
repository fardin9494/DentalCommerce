import { useState } from 'react'
import { PageHeader } from '@/shared/components/PageHeader'
import { TableSkeleton } from '@/shared/components/TableSkeleton'
import {
  useAllWarehouses,
  useCreateWarehouse,
  useUpdateWarehouse,
  useActivateWarehouse,
  useDeactivateWarehouse,
} from '@/shared/hooks/useWarehouses'
import { useConfirm } from '@/shared/components/confirm/ConfirmProvider'
import { CreateWarehouseModal } from '../components/CreateWarehouseModal'
import { EditWarehouseModal } from '../components/EditWarehouseModal'
import type { Warehouse } from '@/shared/api/warehouses'

export function WarehousesPage() {
  const { data: warehouses, isLoading, error } = useAllWarehouses()
  const createWarehouse = useCreateWarehouse()
  const updateWarehouse = useUpdateWarehouse()
  const activateWarehouse = useActivateWarehouse()
  const deactivateWarehouse = useDeactivateWarehouse()
  const confirm = useConfirm()

  const [showCreateModal, setShowCreateModal] = useState(false)
  const [editingWarehouse, setEditingWarehouse] = useState<Warehouse | null>(null)

  async function handleCreate(data: { code: string; name: string }) {
    await createWarehouse.mutateAsync(data)
    setShowCreateModal(false)
  }

  async function handleUpdate(data: { name: string }) {
    if (!editingWarehouse) return
    await updateWarehouse.mutateAsync({ id: editingWarehouse.id, dto: data })
    setEditingWarehouse(null)
  }

  async function handleToggleStatus(warehouse: Warehouse) {
    if (warehouse.isActive) {
      const ok = await confirm.confirm({
        title: 'غیرفعال کردن انبار',
        message: `آیا می‌خواهید انبار "${warehouse.name}" را غیرفعال کنید؟`,
      })
      if (ok) {
        await deactivateWarehouse.mutateAsync(warehouse.id)
      }
    } else {
      await activateWarehouse.mutateAsync(warehouse.id)
    }
  }

  const activeCount = warehouses?.filter((w) => w.isActive).length ?? 0
  const inactiveCount = warehouses?.filter((w) => !w.isActive).length ?? 0

  return (
    <div className="space-y-6">
      <PageHeader
        title="انبارها"
        actions={
          <button
            onClick={() => setShowCreateModal(true)}
            className="inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-emerald-700 focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:ring-offset-2"
          >
            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
            </svg>
            انبار جدید
          </button>
        }
      >
        مدیریت انبارها و مکان‌های نگهداری کالا
      </PageHeader>

      {/* Stats Cards */}
      <div className="grid gap-4 sm:grid-cols-3">
        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
          <div className="text-sm font-medium text-slate-500">کل انبارها</div>
          <div className="mt-2 text-3xl font-bold text-slate-900">{warehouses?.length ?? 0}</div>
        </div>
        <div className="rounded-xl border border-emerald-200 bg-emerald-50 p-4 shadow-sm">
          <div className="text-sm font-medium text-emerald-600">انبارهای فعال</div>
          <div className="mt-2 text-3xl font-bold text-emerald-700">{activeCount}</div>
        </div>
        <div className="rounded-xl border border-slate-200 bg-slate-50 p-4 shadow-sm">
          <div className="text-sm font-medium text-slate-500">انبارهای غیرفعال</div>
          <div className="mt-2 text-3xl font-bold text-slate-600">{inactiveCount}</div>
        </div>
      </div>

      {/* Warehouses List */}
      <div className="rounded-xl border border-slate-200 bg-white shadow-sm overflow-hidden">
        {isLoading ? (
          <TableSkeleton columns={4} rows={8} />
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
        ) : !warehouses || warehouses.length === 0 ? (
          <div className="py-20 text-center">
            <div className="mx-auto mb-4 h-12 w-12 rounded-full bg-slate-100 p-3 text-slate-400">
              <svg fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  d="M2.25 21h19.5m-18-18v18m10.5-18v18m6-13.5V21M6.75 6.75h.75m-.75 3h.75m-.75 3h.75m3-6h.75m-.75 3h.75m-.75 3h.75M6.75 21v-3.375c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125V21M3 3h12m-.75 4.5H21m-3.75 3.75h.008v.008h-.008v-.008Z"
                />
              </svg>
            </div>
            <p className="text-slate-600">انباری یافت نشد</p>
            <p className="mt-1 text-sm text-slate-400">برای شروع یک انبار جدید ایجاد کنید</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-slate-200 bg-slate-50 text-right">
                  <th className="whitespace-nowrap px-6 py-4 font-semibold text-slate-600">کد</th>
                  <th className="whitespace-nowrap px-6 py-4 font-semibold text-slate-600">نام انبار</th>
                  <th className="whitespace-nowrap px-6 py-4 font-semibold text-slate-600">وضعیت</th>
                  <th className="whitespace-nowrap px-6 py-4 font-semibold text-slate-600">عملیات</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {warehouses.map((warehouse) => (
                  <tr key={warehouse.id} className="transition-colors hover:bg-slate-50">
                    <td className="whitespace-nowrap px-6 py-4">
                      <span className="inline-flex items-center rounded-lg bg-slate-100 px-2.5 py-1 font-mono text-sm font-medium text-slate-700">
                        {warehouse.code}
                      </span>
                    </td>
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
                              d="M2.25 21h19.5m-18-18v18m10.5-18v18m6-13.5V21M6.75 6.75h.75m-.75 3h.75m-.75 3h.75m3-6h.75m-.75 3h.75m-.75 3h.75M6.75 21v-3.375c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125V21M3 3h12m-.75 4.5H21m-3.75 3.75h.008v.008h-.008v-.008Z"
                            />
                          </svg>
                        </div>
                        <span className="font-medium text-slate-900">{warehouse.name}</span>
                      </div>
                    </td>
                    <td className="whitespace-nowrap px-6 py-4">
                      <button
                        onClick={() => handleToggleStatus(warehouse)}
                        disabled={activateWarehouse.isPending || deactivateWarehouse.isPending}
                        className={`inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-xs font-medium transition-colors ${
                          warehouse.isActive
                            ? 'bg-emerald-100 text-emerald-700 hover:bg-emerald-200'
                            : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
                        }`}
                      >
                        <span
                          className={`h-1.5 w-1.5 rounded-full ${warehouse.isActive ? 'bg-emerald-500' : 'bg-slate-400'}`}
                        />
                        {warehouse.isActive ? 'فعال' : 'غیرفعال'}
                      </button>
                    </td>
                    <td className="whitespace-nowrap px-6 py-4">
                      <div className="flex items-center gap-1">
                        <button
                          onClick={() => setEditingWarehouse(warehouse)}
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
                        {warehouse.isActive ? (
                          <button
                            onClick={() => handleToggleStatus(warehouse)}
                            disabled={deactivateWarehouse.isPending}
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
                        ) : (
                          <button
                            onClick={() => handleToggleStatus(warehouse)}
                            disabled={activateWarehouse.isPending}
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

      {/* Create Modal */}
      <CreateWarehouseModal
        isOpen={showCreateModal}
        onClose={() => setShowCreateModal(false)}
        onSubmit={handleCreate}
        isSubmitting={createWarehouse.isPending}
      />

      {/* Edit Modal */}
      <EditWarehouseModal
        isOpen={!!editingWarehouse}
        warehouse={editingWarehouse}
        onClose={() => setEditingWarehouse(null)}
        onSubmit={handleUpdate}
        isSubmitting={updateWarehouse.isPending}
      />
    </div>
  )
}
