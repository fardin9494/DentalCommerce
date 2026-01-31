import { CATALOG_API_BASE } from '@/app/env'
import { fetchJsonWithBase, toQuery } from '@/lib/api/client'
import type { CatalogBrandListItem, CatalogCategoryLeaf, CatalogProductDetail, CatalogProductListResult, CatalogStoreListItem } from './catalogTypes'

export async function searchCatalogProducts(params: { search: string; page?: number; pageSize?: number }) {
  return fetchJsonWithBase<CatalogProductListResult>(
    CATALOG_API_BASE,
    `/products${toQuery({ page: params.page ?? 1, pageSize: params.pageSize ?? 20, search: params.search })}`,
  )
}

export async function getCatalogProduct(id: string) {
  return fetchJsonWithBase<CatalogProductDetail>(CATALOG_API_BASE, `/products/${id}`)
}

export async function listCatalogStores(search?: string) {
  return fetchJsonWithBase<CatalogStoreListItem[]>(
    CATALOG_API_BASE,
    `/stores${toQuery({ search })}`,
  )
}

export async function listCatalogBrands(search?: string) {
  return fetchJsonWithBase<CatalogBrandListItem[]>(
    CATALOG_API_BASE,
    `/brands${toQuery({ search })}`,
  )
}

export async function listCatalogCategoryLeaves() {
  return fetchJsonWithBase<CatalogCategoryLeaf[]>(
    CATALOG_API_BASE,
    `/categories/leaves`,
  )
}
