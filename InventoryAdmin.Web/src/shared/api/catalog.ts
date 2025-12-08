import { z } from 'zod'
import { fetchJson, toQuery } from '@/lib/api/client'

// Product list item schema
export const ProductListItemSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  code: z.string(),
  defaultSlug: z.string(),
  brandId: z.string().uuid().nullable().optional(),
  brandName: z.string().nullable().optional(),
  primaryCategoryId: z.string().uuid().nullable().optional(),
  status: z.string(),
  mainImageId: z.string().uuid().nullable().optional(),
  mainImageUrl: z.string().nullable().optional(),
  createdAt: z.string(),
  updatedAt: z.string(),
})
export type ProductListItem = z.infer<typeof ProductListItemSchema>

// Products list result
export const ProductsListResultSchema = z.object({
  page: z.number(),
  pageSize: z.number(),
  total: z.number(),
  items: z.array(ProductListItemSchema),
})
export type ProductsListResult = z.infer<typeof ProductsListResultSchema>

// Variant schema
export const VariantSchema = z.object({
  id: z.string().uuid(),
  value: z.string(),
  sku: z.string(),
  isActive: z.boolean(),
})
export type Variant = z.infer<typeof VariantSchema>

// Product detail (with variants)
export const ProductDetailSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  code: z.string(),
  brandName: z.string().nullable().optional(),
  status: z.string(),
  variants: z.array(VariantSchema).optional().default([]),
})
export type ProductDetail = z.infer<typeof ProductDetailSchema>

// Search products
export async function searchProducts(params: {
  search?: string
  page?: number
  pageSize?: number
}): Promise<ProductsListResult> {
  const query = toQuery(params)
  const data = await fetchJson<unknown>(`/catalog/products${query}`)
  return ProductsListResultSchema.parse(data)
}

// Get product detail with variants
export async function getProductDetail(id: string): Promise<ProductDetail> {
  const data = await fetchJson<unknown>(`/catalog/products/${id}`)
  return ProductDetailSchema.parse(data)
}

