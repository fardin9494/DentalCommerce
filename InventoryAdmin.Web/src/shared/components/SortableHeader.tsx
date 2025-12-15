import React from 'react'

interface SortableHeaderProps {
    label: string
    sortKey: string
    currentSortKey: string | null
    currentDirection: 'asc' | 'desc' | null
    onSort: (key: string) => void
    className?: string
}

export function SortableHeader({
    label,
    sortKey,
    currentSortKey,
    currentDirection,
    onSort,
    className = '',
}: SortableHeaderProps) {
    const isActive = currentSortKey === sortKey

    return (
        <th
            className={`whitespace-nowrap px-4 py-3 font-semibold text-slate-600 cursor-pointer select-none hover:bg-slate-100 transition-colors ${className}`}
            onClick={() => onSort(sortKey)}
        >
            <div className="flex items-center gap-1.5">
                <span>{label}</span>
                <span className="flex flex-col text-[10px] leading-none">
                    <svg
                        className={`h-2.5 w-2.5 transition-colors ${isActive && currentDirection === 'asc' ? 'text-emerald-600' : 'text-slate-300'
                            }`}
                        fill="currentColor"
                        viewBox="0 0 24 24"
                    >
                        <path d="M12 5l8 8H4z" />
                    </svg>
                    <svg
                        className={`h-2.5 w-2.5 -mt-0.5 transition-colors ${isActive && currentDirection === 'desc' ? 'text-emerald-600' : 'text-slate-300'
                            }`}
                        fill="currentColor"
                        viewBox="0 0 24 24"
                    >
                        <path d="M12 19l-8-8h16z" />
                    </svg>
                </span>
            </div>
        </th>
    )
}
