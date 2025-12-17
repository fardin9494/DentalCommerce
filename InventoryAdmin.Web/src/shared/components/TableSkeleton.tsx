interface TableSkeletonProps {
  /**
   * تعداد ستون‌های جدول
   */
  columns?: number
  /**
   * تعداد ردیف‌های skeleton (پیش‌فرض: 5)
   */
  rows?: number
  /**
   * آیا header هم نمایش داده شود (پیش‌فرض: true)
   */
  showHeader?: boolean
  /**
   * عرض‌های سفارشی برای هر ستون (اختیاری)
   */
  columnWidths?: string[]
}

export function TableSkeleton({
  columns = 5,
  rows = 5,
  showHeader = true,
  columnWidths,
}: TableSkeletonProps) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        {showHeader && (
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50 text-right">
              {Array.from({ length: columns }, (_, i) => (
                <th key={i} className="whitespace-nowrap px-4 py-3">
                  <div
                    className={`h-4 bg-slate-200 rounded animate-pulse ${
                      columnWidths?.[i] || 'w-24'
                    }`}
                  />
                </th>
              ))}
            </tr>
          </thead>
        )}
        <tbody className="divide-y divide-slate-100">
          {Array.from({ length: rows }, (_, rowIndex) => (
            <tr key={rowIndex} className="transition-colors">
              {Array.from({ length: columns }, (_, colIndex) => (
                <td key={colIndex} className="px-4 py-3">
                  <div
                    className={`h-4 bg-slate-200 rounded animate-pulse ${
                      columnWidths?.[colIndex] || 'w-full'
                    }`}
                  />
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

