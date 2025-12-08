import { useQuery } from '@tanstack/react-query'
import { searchProducts, getProductDetail } from '../api/catalog'

export function useProductSearch(search: string, page = 1, pageSize = 10, enabled = true) {
  return useQuery({
    queryKey: ['catalog', 'products', 'search', { search, page, pageSize }],
    queryFn: () => searchProducts({ search, page, pageSize }),
    enabled: enabled && search.length >= 2, // Only search when 2+ chars
    staleTime: 30 * 1000, // Cache for 30 seconds
  })
}

export function useProductDetail(id: string | undefined, enabled = true) {
  return useQuery({
    queryKey: ['catalog', 'products', 'detail', id],
    queryFn: () => getProductDetail(id!),
    enabled: enabled && !!id,
    staleTime: 60 * 1000, // Cache for 1 minute
  })
}

