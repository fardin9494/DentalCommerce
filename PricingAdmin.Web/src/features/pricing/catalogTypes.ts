export type CatalogProductListItem = {
  id: string
  name: string
  code?: string | null
  brandName?: string | null
  status?: string | null
  mainImageUrl?: string | null
}

export type CatalogProductListResult = {
  page: number
  pageSize: number
  total: number
  items: CatalogProductListItem[]
}

export type CatalogProductVariant = {
  id: string
  value: string
  sku: string
  isActive: boolean
}

export type CatalogBrandListItem = {
  id: string
  name: string
  status?: string | null
  productsCount?: number | null
}

export type CatalogCategoryLeaf = {
  id: string
  name: string
  slug: string
  parentId?: string | null
  depth: number
}

export type CatalogStoreListItem = {
  id: string
  name: string
  domain?: string | null
}

export type CatalogProductStore = {
  storeId: string
  isVisible: boolean
  slug?: string | null
  titleOverride?: string | null
  descriptionOverride?: string | null
}

export type CatalogProductDetail = {
  id: string
  name: string
  code?: string | null
  brandName?: string | null
  status?: string | null
  variants: CatalogProductVariant[]
  stores: CatalogProductStore[]
}
