import { useState, useMemo, useEffect } from 'react'
import { Spinner } from '@/shared/components/Spinner'
import { useShelves } from '../queries'
import { useActiveWarehouses, useWarehouseNames } from '@/shared/hooks/useWarehouses'
import { ShelfProductsModal } from './ShelfProductsModal'
import type { Shelf } from '../api'

interface VisualShelvesViewProps {
  isOpen: boolean
  onClose: () => void
}

export function VisualShelvesView({ isOpen, onClose }: VisualShelvesViewProps) {
  const [selectedWarehouseId, setSelectedWarehouseId] = useState<string>('')
  const [selectedShelf, setSelectedShelf] = useState<Shelf | null>(null)
  const { data: warehouses } = useActiveWarehouses()
  const { getWarehouseName } = useWarehouseNames()
  
  // Only fetch shelves when warehouse is selected and modal is open
  const { data: shelves, isLoading } = useShelves(
    selectedWarehouseId || undefined,
    selectedWarehouseId ? true : undefined,
    isOpen && !!selectedWarehouseId
  )

  // Reset state when modal closes
  useEffect(() => {
    if (!isOpen) {
      setSelectedWarehouseId('')
      setSelectedShelf(null)
    }
  }, [isOpen])

  // Group shelves by row and column (each position can have multiple levels)
  const shelvesByPosition = useMemo(() => {
    if (!shelves) return new Map<string, Shelf[]>()
    const grouped = new Map<string, Shelf[]>()
    shelves.forEach((shelf) => {
      const key = `${shelf.rowNumber}-${shelf.columnNumber}`
      if (!grouped.has(key)) {
        grouped.set(key, [])
      }
      grouped.get(key)!.push(shelf)
    })
    // Sort shelves by level number within each position
    grouped.forEach((shelfList) => {
      shelfList.sort((a, b) => a.levelNumber - b.levelNumber)
    })
    return grouped
  }, [shelves])

  // Calculate grid dimensions (based on all shelves, not just one level)
  const gridDimensions = useMemo(() => {
    if (!selectedWarehouseId || !shelves || shelves.length === 0) {
      return { maxRow: 0, maxColumn: 0 }
    }
    const maxRow = Math.max(...shelves.map((s) => s.rowNumber))
    const maxColumn = Math.max(...shelves.map((s) => s.columnNumber))
    return { maxRow, maxColumn }
  }, [selectedWarehouseId, shelves])

  function handleWarehouseSelect(warehouseId: string) {
    setSelectedWarehouseId(warehouseId)
    setSelectedShelf(null)
  }

  function handleShelfClick(shelf: Shelf) {
    setSelectedShelf(shelf)
  }

  if (!isOpen) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
      <div className="w-full max-w-7xl max-h-[95vh] rounded-xl bg-white shadow-xl flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between border-b border-slate-200 px-6 py-4">
          <div>
            <h3 className="text-xl font-bold text-slate-900">نمای بصری قفسه‌ها</h3>
            <p className="mt-1 text-sm text-slate-500">
              {selectedWarehouseId
                ? `انبار: ${getWarehouseName(selectedWarehouseId)}`
                : 'لطفاً انبار را انتخاب کنید'}
            </p>
          </div>
          <button
            onClick={onClose}
            className="rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-600"
          >
            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" d="M6 18 18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {/* Content */}
        <div className="flex-1 overflow-hidden flex flex-col">
          {/* Warehouse Selection */}
          {!selectedWarehouseId ? (
            <div className="flex-1 flex items-center justify-center p-8">
              <div className="w-full max-w-md">
                <label className="mb-2 block text-sm font-medium text-slate-700">انتخاب انبار</label>
                <select
                  value={selectedWarehouseId}
                  onChange={(e) => handleWarehouseSelect(e.target.value)}
                  className="w-full rounded-lg border border-slate-300 py-3 px-4 text-sm focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20"
                >
                  <option value="">-- انتخاب انبار --</option>
                  {warehouses?.map((w) => (
                    <option key={w.id} value={w.id}>
                      {w.name}
                    </option>
                  ))}
                </select>
              </div>
            </div>
          ) : isLoading ? (
            <div className="flex-1 flex items-center justify-center">
              <Spinner className="h-8 w-8 text-emerald-600" />
            </div>
          ) : (
            <>
              {/* Grid View */}
              <div className="flex-1 overflow-auto p-6">
                {gridDimensions.maxRow === 0 || gridDimensions.maxColumn === 0 ? (
                  <div className="flex items-center justify-center h-full">
                    <div className="text-center">
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
                    </div>
                  </div>
                ) : (
                  <div className="space-y-4">
                    {/* Column Headers */}
                    <div className="flex gap-2">
                      <div className="w-16"></div>
                      {Array.from({ length: gridDimensions.maxColumn }, (_, i) => i + 1).map((col) => (
                        <div
                          key={col}
                          className="flex-1 text-center text-xs font-medium text-slate-500 py-2"
                        >
                          ستون {col.toLocaleString('fa-IR')}
                        </div>
                      ))}
                    </div>

                    {/* Grid Rows */}
                    {Array.from({ length: gridDimensions.maxRow }, (_, i) => i + 1).map((row) => (
                      <div key={row} className="flex gap-2">
                        {/* Row Header */}
                        <div className="w-16 flex items-center justify-center text-xs font-medium text-slate-500">
                          ردیف {row.toLocaleString('fa-IR')}
                        </div>

                        {/* Grid Cells */}
                        {Array.from({ length: gridDimensions.maxColumn }, (_, i) => i + 1).map((col) => {
                          const key = `${row}-${col}`
                          const positionShelves = shelvesByPosition.get(key) || []

                          return (
                            <div
                              key={col}
                              className="flex-1 min-h-[80px] border-2 rounded-lg transition-all"
                              style={{
                                borderColor: positionShelves.length > 0
                                  ? 'rgb(226, 232, 240)'
                                  : 'rgb(226, 232, 240)',
                                backgroundColor: 'rgb(248, 250, 252)',
                              }}
                            >
                              {positionShelves.length > 0 ? (
                                <div className="h-full flex flex-col gap-1 p-1">
                                  {positionShelves.map((shelf) => (
                                    <div
                                      key={shelf.id}
                                      className="flex-1 min-h-[60px] rounded border transition-all cursor-pointer"
                                      style={{
                                        borderColor: shelf.isActive
                                          ? 'rgb(16, 185, 129)'
                                          : 'rgb(148, 163, 184)',
                                        backgroundColor: shelf.isActive
                                          ? 'rgba(16, 185, 129, 0.1)'
                                          : 'rgba(148, 163, 184, 0.1)',
                                      }}
                                      onClick={() => handleShelfClick(shelf)}
                                    >
                                      <div className="h-full flex flex-col items-center justify-center p-1.5 hover:bg-emerald-50/50 rounded transition-colors">
                                        <div className="text-[10px] font-semibold text-emerald-700 mb-0.5">
                                          طبقه {shelf.levelNumber.toLocaleString('fa-IR')}
                                        </div>
                                        <div className="text-xs font-semibold text-slate-900 text-center">
                                          {shelf.name}
                                        </div>
                                        <div className="text-[9px] text-slate-500 mt-0.5 font-mono">
                                          {shelf.code}
                                        </div>
                                        {!shelf.isActive && (
                                          <div className="mt-0.5 text-[9px] text-slate-400">غیرفعال</div>
                                        )}
                                      </div>
                                    </div>
                                  ))}
                                </div>
                              ) : (
                                <div className="h-full flex items-center justify-center">
                                  <span className="text-xs text-slate-300">-</span>
                                </div>
                              )}
                            </div>
                          )
                        })}
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </>
          )}
        </div>

        {/* Footer */}
        <div className="border-t border-slate-200 px-6 py-4 flex items-center justify-between">
          <div className="text-sm text-slate-600">
            {selectedWarehouseId && shelves && (
              <>
                {shelves.length} قفسه
              </>
            )}
          </div>
          <div className="flex gap-2">
            {selectedWarehouseId && (
              <button
                onClick={() => {
                  setSelectedWarehouseId('')
                  setSelectedShelf(null)
                }}
                className="rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-50"
              >
                تغییر انبار
              </button>
            )}
            <button
              onClick={onClose}
              className="rounded-lg bg-slate-100 px-4 py-2 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-200"
            >
              بستن
            </button>
          </div>
        </div>
      </div>

      {/* Shelf Products Modal */}
      <ShelfProductsModal
        isOpen={!!selectedShelf}
        shelf={selectedShelf}
        onClose={() => setSelectedShelf(null)}
      />
    </div>
  )
}
