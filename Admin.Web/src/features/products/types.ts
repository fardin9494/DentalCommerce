import { z } from 'zod'

// Country (from ListCountriesQuery)
export const CountrySchema = z.object({
  code2: z.string(),
  code3: z.string(),
  nameFa: z.string(),
  nameEn: z.string(),
  region: z.string().nullable().optional(),
  flagEmoji: z.string().nullable().optional(),
})
export type Country = z.infer<typeof CountrySchema>

// Leaf category item (server-side leaf list)
export const LeafCategorySchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  slug: z.string(),
  parentId: z.string().uuid().nullable().optional(),
  depth: z.number(),
})
export type LeafCategory = z.infer<typeof LeafCategorySchema>

// Store list item
export const StoreSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  domain: z.string().nullable().optional(),
})
export type Store = z.infer<typeof StoreSchema>

// Brand list item (from ListBrandsQuery)
export const BrandSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  website: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  establishedYear: z.number().nullable().optional(),
  logoMediaId: z.string().uuid().nullable().optional(),
  logoUrl: z.string().nullable().optional(),
  status: z.any(),
  productsCount: z.number(),
})
export type Brand = z.infer<typeof BrandSchema>

export const BrandDetailSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  normalizedName: z.string(),
  website: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  establishedYear: z.number().nullable().optional(),
  logoMediaId: z.string().uuid().nullable().optional(),
  logoUrl: z.string().nullable().optional(),
  status: z.number(),
})
export type BrandDetail = z.infer<typeof BrandDetailSchema>

// Category tree node
export const CategoryNodeSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  slug: z.string(),
  parentId: z.string().uuid().nullable().optional(),
  depth: z.number(),
})
export type CategoryNode = z.infer<typeof CategoryNodeSchema>

// Product list item (ProductListItemDto)
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
  mainImageUrl: z.string().url().nullable().optional(),
  createdAt: z.string(),
  updatedAt: z.string(),
})
export type ProductListItem = z.infer<typeof ProductListItemSchema>

// Product detail (ProductDetailDto)
export const ProductImageSchema = z.object({
  id: z.string().uuid(),
  url: z.string(),
  alt: z.string().nullable().optional(),
  sortOrder: z.number(),
  isMain: z.boolean(),
})
export type ProductImage = z.infer<typeof ProductImageSchema>

export const ProductDetailSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  code: z.string(),
  defaultSlug: z.string(),
  brandName: z.string(),
  description: z.string(),
  brandId: z.string().uuid(),
  status: z.string(),
  warehouseCode: z.string().nullable().optional(),
  countryCode: z.string().length(2).nullable().optional(),
  primaryCategoryId: z.string().uuid().nullable().optional(),
  variationKey: z.string().nullable().optional(),
  images: z.array(ProductImageSchema),
  variants: z.array(z.object({ id: z.string().uuid(), value: z.string(), sku: z.string(), isActive: z.boolean() })),
  properties: z.array(z.object({
    id: z.string().uuid(), key: z.string(), valueString: z.string().nullable().optional(), valueDecimal: z.number().nullable().optional(), valueBool: z.boolean().nullable().optional(), valueJson: z.string().nullable().optional(),
  })),
  categories: z.array(z.object({ categoryId: z.string().uuid(), isPrimary: z.boolean() })),
  stores: z.array(z.object({
    storeId: z.string().uuid(), isVisible: z.boolean(), slug: z.string(), titleOverride: z.string().nullable().optional(), descriptionOverride: z.string().nullable().optional(),
  })),
  seos: z.array(z.object({
    storeId: z.string().uuid(), metaTitle: z.string().nullable().optional(), metaDescription: z.string().nullable().optional(), canonicalUrl: z.string().nullable().optional(), robots: z.string().nullable().optional(), jsonLd: z.string().nullable().optional(),
  })),
})
export type ProductDetail = z.infer<typeof ProductDetailSchema>

// CreateProductCommand DTO
export const ProductCreateSchema = z.object({
  name: z.string().min(1, 'نام اجباری است'),
  slug: z.string().min(1, 'اسلاگ اجباری است'),
  code: z.string().min(1, 'کد کالا اجباری است'),
  brandId: z.string().uuid({ message: 'برند را انتخاب کنید' }),
  categoryIds: z.array(z.string().uuid()).min(1, 'حداقل یک دسته بندی انتخاب کنید'),
  warehouseCode: z.string().optional().nullable(),
  variationKey: z.string().optional().nullable(),
  countryCode: z.string().length(2).optional().nullable(),
})
export type ProductCreateDto = z.infer<typeof ProductCreateSchema>
