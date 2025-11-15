import { fetchJson, toQuery } from '../../lib/api/client'
import { API_BASE } from '../../app/env'
import type { Paginated } from '../../lib/api/types'
import { BrandSchema, BrandDetailSchema, CategoryNodeSchema, CountrySchema, StoreSchema, LeafCategorySchema, ProductCreateSchema, ProductDetailSchema, ProductListItemSchema, type ProductDetail, type ProductListItem, type ProductCreateDto, type BrandDetail } from './types'
import { swalConfirm } from '../../shared/utils/swal'

export type ListParams = {
  page?: number
  pageSize?: number
  search?: string
  brandId?: string
  categoryId?: string
  storeId?: string
  visibleInStore?: boolean
  isActive?: boolean
  sort?: string
}

export async function listProducts(params: ListParams): Promise<{ items: ProductListItem[]; page: number; pageSize: number; total: number; totalPages: number }> {
  const data = await fetchJson<any>(`/products${toQuery(params)}`)
  const items = (data.items as unknown[]).map((p) => ProductListItemSchema.parse(p))
  const page = data.page ?? 1
  const pageSize = data.pageSize ?? items.length
  const total = data.total ?? items.length
  const totalPages = Math.max(1, Math.ceil(total / Math.max(1, pageSize)))
  return { items, page, pageSize, total, totalPages }
}

export async function getProduct(id: string): Promise<ProductDetail> {
  const data = await fetchJson<unknown>(`/products/${id}`)
  return ProductDetailSchema.parse(data)
}

export async function createProduct(dto: ProductCreateDto): Promise<{ id: string }> {
  const payload = ProductCreateSchema.parse(dto)
  // returns { id }
  return fetchJson<{ id: string }>(`/products`, { method: 'POST', json: payload })
}

// No generic update/delete endpoints for product in backend; specific endpoints exist

export async function uploadProductImage(id: string, file: File, alt?: string): Promise<{ id: string }> {
  if (!file || file.size === 0) throw new Error('فایل انتخاب نشده است')
  const maxBytes = 10 * 1024 * 1024
  if (file.size > maxBytes) throw new Error('حداکثر اندازه مجاز 10MB است')
  const form = new FormData()
  form.append('file', file)
  if (alt) form.append('alt', alt)
  const base = (API_BASE || '').replace(/\/$/, '')
  const res = await fetch(`${base}/products/${id}/images/upload`, {
    method: 'POST',
    body: form,
  })
  if (!res.ok) {
    try {
      const data = await res.json()
      const msg = data?.detail || data?.title || data?.message || data?.error || (typeof data === 'string' ? data : '')
      throw new Error(msg || `Upload failed (${res.status})`)
    } catch {
      const txt = await res.text().catch(() => '')
      throw new Error(txt || `Upload failed (${res.status})`)
    }
  }
  return res.json()
}

export async function listBrands() {
  const data = await fetchJson<unknown[]>(`/brands`)
  return data.map((b) => BrandSchema.parse(b))
}

export async function getBrand(id: string): Promise<BrandDetail> {
  const data = await fetchJson<unknown>(`/brands/${id}`)
  return BrandDetailSchema.parse(data)
}

export type BrandStatusValue = 1 | 2 | 3

export type CreateBrandInput = {
  name: string
  website?: string | null
  description?: string | null
  establishedYear?: number | null
  logoMediaId?: string | null
  status?: BrandStatusValue | null
}

export type UpdateBrandInput = {
  name: string
  website?: string | null
  description?: string | null
  establishedYear?: number | null
  status: BrandStatusValue
}

export type BrandLogoResponse = {
  logoMediaId: string
  logoUrl: string
}

export async function listStores(search?: string) {
  const data = await fetchJson<unknown[]>(`/stores${toQuery({ search })}`)
  return data.map((s) => StoreSchema.parse(s))
}

export async function listCountries() {
  const data = await fetchJson<unknown[]>(`/countries`)
  return data.map((c) => CountrySchema.parse(c))
}

export async function listCategories() {
  const data = await fetchJson<unknown[]>(`/categories/tree`)
  return data.map((c) => CategoryNodeSchema.parse(c))
}

export async function listLeafCategories() {
  const data = await fetchJson<unknown[]>(`/categories/leaves`)
  return data.map((c) => LeafCategorySchema.parse(c))
}

// Leaves that have products (blocked as drop targets)
export async function listLeafCategoriesWithProducts() {
  const data = await fetchJson<unknown[]>(`/categories/leaves/with-products`)
  return data.map((c) => LeafCategorySchema.parse(c))
}

export async function createBrand(input: CreateBrandInput) {
  return fetchJson<{ id: string }>(`/brands`, { method: 'POST', json: input })
}

export async function renameBrand(id: string, name: string) {
  return fetchJson<void>(`/brands/${id}/rename`, { method: 'POST', json: { name } as any })
}

export async function updateBrand(id: string, input: UpdateBrandInput) {
  return fetchJson<void>(`/brands/${id}`, { method: 'PUT', json: input })
}

export async function uploadBrandLogo(id: string, file: File) {
  if (!file || file.size === 0) throw new Error('فایل انتخاب نشده است')
  const form = new FormData()
  form.append('file', file)
  const base = (API_BASE || '').replace(/\/$/, '')
  const res = await fetch(`${base}/brands/${id}/logo`, {
    method: 'POST',
    body: form,
  })
  if (!res.ok) {
    const text = await res.text().catch(() => '')
    throw new Error(text || `Upload failed (${res.status})`)
  }
  return (await res.json()) as BrandLogoResponse
}
export async function deleteBrand(id: string) {
  return fetchJson<void>(`/brands/${id}`, { method: 'DELETE' })
}

export async function createStore(input: { name: string; domain?: string | null }) {
  return fetchJson<{ id: string }>(`/stores`, { method: 'POST', json: input })
}

export async function renameStore(id: string, name: string) {
  return fetchJson<void>(`/stores/${id}/rename`, { method: 'POST', json: { name } as any })
}

export async function setStoreDomain(id: string, domain: string | null) {
  return fetchJson<void>(`/stores/${id}/domain`, { method: 'POST', json: { domain } as any })
}

export async function createCountry(input: { code2: string; code3: string; nameFa: string; nameEn: string; region?: string | null; flagEmoji?: string | null }) {
  return fetchJson<{ code2: string }>(`/countries`, { method: 'POST', json: input })
}

export async function updateCountry(code2: string, input: { nameFa: string; nameEn: string; region?: string | null; flagEmoji?: string | null }) {
  return fetchJson<void>(`/countries/${code2}`, { method: 'POST', json: { ...input } as any })
}

export async function deleteCountry(code2: string) {
  return fetchJson<void>(`/countries/${code2}`, { method: 'DELETE' })
}

export async function createCategory(input: { name: string; slug: string; parentId?: string | null }) {
  return fetchJson<{ id: string }>(`/categories`, { method: 'POST', json: input })
}

export async function renameCategory(id: string, input: { name: string; slug: string }) {
  return fetchJson<void>(`/categories/${id}/rename`, { method: 'POST', json: input })
}

export async function moveCategory(id: string, newParentId: string | null) {
  return fetchJson<void>(`/categories/${id}/move`, { method: 'POST', json: { newParentId } as any })
}

// Product extras
export async function setProductDescription(id: string, contentHtml: string) {
  return fetchJson<void>(`/products/${id}/description`, { method: 'POST', json: { contentHtml } as any })
}

export async function upsertProductSeo(id: string, input: { storeId: string; metaTitle?: string | null; metaDescription?: string | null; canonicalUrl?: string | null; robots?: string | null; jsonLd?: string | null }) {
  return fetchJson<void>(`/products/${id}/seo`, { method: 'POST', json: input as any })
}

export async function upsertProductStore(id: string, input: { storeId: string; isVisible: boolean; slug: string; titleOverride?: string | null; descriptionOverride?: string | null }) {
  return fetchJson<void>(`/products/${id}/stores`, { method: 'POST', json: input as any })
}

export async function deleteProductStore(id: string, storeId: string) {
  try {
    return await fetchJson<void>(`/products/${id}/stores/${storeId}`, { method: 'DELETE' })
  } catch (e) {
    // Fallback for environments where DELETE may be blocked or route missing
    return fetchJson<void>(`/products/${id}/stores/remove`, { method: 'POST', json: { storeId } as any })
  }
}

export async function upsertProductProperty(id: string, input: { key: string; valueString?: string | null; valueDecimal?: number | null; valueBool?: boolean | null; valueJson?: string | null }) {
  return fetchJson<{ propertyId: string }>(`/products/${id}/properties`, { method: 'POST', json: input as any })
}

export async function setVariation(id: string, variationKey: string | null) {
  return fetchJson<void>(`/products/${id}/variation`, { method: 'POST', json: { variationKey } as any })
}

export async function upsertVariant(id: string, input: { variantValue: string; sku: string; isActive: boolean }) {
  return fetchJson<{ variantId: string }>(`/products/${id}/variants`, { method: 'POST', json: input as any })
}

export async function deleteVariant(productId: string, variantId: string) {
  return fetchJson<void>(`/products/${productId}/variants/${variantId}`, { method: 'DELETE' })
}

// Update existing variant by id (value, sku, isActive)
export async function updateVariant(productId: string, variantId: string, input: { variantValue: string; sku: string; isActive: boolean }) {
  try {
    return await fetchJson<void>(`/products/${productId}/variants/${variantId}`, { method: 'POST', json: input as any })
  } catch (e: any) {
    // Fallback: delete + upsert, in case server route not available or update by id is restricted.
    const status = e?.status as number | undefined
    if (status && (status === 404 || status === 405)) {
      try {
        await deleteVariant(productId, variantId)
        await upsertVariant(productId, input)
        return
      } catch (inner) {
        throw inner
      }
    }
    // Surface backend problem details detail if present
    const detail = e?.details?.detail || e?.message || 'Update failed'
    throw new Error(detail)
  }
}

export async function activateProduct(id: string) {
  return fetchJson<void>(`/products/${id}/activate`, { method: 'POST' })
}

export async function hideProduct(id: string) {
  return fetchJson<void>(`/products/${id}/hide`, { method: 'POST' })
}

export async function setMainImage(productId: string, imageId: string) {
  return fetchJson<void>(`/products/${productId}/images/${imageId}/main`, { method: 'POST' })
}

export async function reorderImages(productId: string, orderedIds: string[]) {
  return fetchJson<void>(`/products/${productId}/images/reorder`, { method: 'POST', json: orderedIds })
}

export async function deleteImage(productId: string, imageId: string) {
  return fetchJson<void>(`/products/${productId}/images/${imageId}`, { method: 'DELETE' })
}

export async function updateProductBasics(id: string, input: { name: string; slug: string; code: string; brandId: string; warehouseCode?: string | null; countryCode?: string | null }) {
  return fetchJson<void>(`/products/${id}/basics`, { method: 'POST', json: input })
}

export async function setProductCategories(id: string, input: { categoryIds: string[]; primaryCategoryId?: string | null }) {
  return fetchJson<void>(`/products/${id}/categories`, { method: 'POST', json: input })
}

export async function setPrimaryCategory(id: string, categoryId: string) {
  return fetchJson<void>(`/products/${id}/categories/primary`, { method: 'POST', json: categoryId as any })
}

export async function deleteProperty(productId: string, propertyId: string) {
  const ok = await swalConfirm({ title: 'حذف ویژگی', text: 'آیا از حذف این ویژگی مطمئن هستید؟', icon: 'warning', confirmText: 'بله، حذف کن', cancelText: 'خیر' })
  if (!ok) throw new Error('__CANCELLED__')
  return fetchJson<void>(`/products/${productId}/properties/${propertyId}`, { method: 'DELETE' })
}
