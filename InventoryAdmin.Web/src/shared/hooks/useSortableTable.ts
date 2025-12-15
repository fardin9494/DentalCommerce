import { useState, useMemo, useCallback } from 'react'

export type SortDirection = 'asc' | 'desc'

export interface SortConfig<T> {
    key: keyof T | null
    direction: SortDirection
}

export interface UseSortableTableOptions<T> {
    data: T[]
    defaultSortKey?: keyof T
    defaultDirection?: SortDirection
}

export interface UseSortableTableReturn<T> {
    sortedData: T[]
    sortConfig: SortConfig<T>
    requestSort: (key: keyof T) => void
    getSortIndicator: (key: keyof T) => 'asc' | 'desc' | null
}

export function useSortableTable<T>({
    data,
    defaultSortKey,
    defaultDirection = 'desc',
}: UseSortableTableOptions<T>): UseSortableTableReturn<T> {
    const [sortConfig, setSortConfig] = useState<SortConfig<T>>({
        key: defaultSortKey || null,
        direction: defaultDirection,
    })

    const sortedData = useMemo(() => {
        if (!sortConfig.key || !data) return data || []

        const sorted = [...data].sort((a, b) => {
            const aValue = a[sortConfig.key!]
            const bValue = b[sortConfig.key!]

            // Handle null/undefined
            if (aValue == null && bValue == null) return 0
            if (aValue == null) return sortConfig.direction === 'asc' ? -1 : 1
            if (bValue == null) return sortConfig.direction === 'asc' ? 1 : -1

            // Handle dates
            if (typeof aValue === 'string' && typeof bValue === 'string') {
                // Try to parse as date
                const aDate = new Date(aValue)
                const bDate = new Date(bValue)
                if (!isNaN(aDate.getTime()) && !isNaN(bDate.getTime())) {
                    return sortConfig.direction === 'asc'
                        ? aDate.getTime() - bDate.getTime()
                        : bDate.getTime() - aDate.getTime()
                }
                // String comparison (case-insensitive)
                const comparison = aValue.localeCompare(bValue, 'fa')
                return sortConfig.direction === 'asc' ? comparison : -comparison
            }

            // Handle numbers
            if (typeof aValue === 'number' && typeof bValue === 'number') {
                return sortConfig.direction === 'asc' ? aValue - bValue : bValue - aValue
            }

            // Default comparison
            if (aValue < bValue) return sortConfig.direction === 'asc' ? -1 : 1
            if (aValue > bValue) return sortConfig.direction === 'asc' ? 1 : -1
            return 0
        })

        return sorted
    }, [data, sortConfig])

    const requestSort = useCallback((key: keyof T) => {
        setSortConfig((prev) => {
            if (prev.key === key) {
                // Toggle direction
                return { key, direction: prev.direction === 'asc' ? 'desc' : 'asc' }
            }
            // New column, default to desc for dates, asc for others
            return { key, direction: 'desc' }
        })
    }, [])

    const getSortIndicator = useCallback(
        (key: keyof T): 'asc' | 'desc' | null => {
            if (sortConfig.key === key) {
                return sortConfig.direction
            }
            return null
        },
        [sortConfig]
    )

    return { sortedData, sortConfig, requestSort, getSortIndicator }
}
