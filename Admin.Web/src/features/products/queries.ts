import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { swalConfirm } from '../../shared/utils/swal'
import * as api from './api'

export function useProducts(params: api.ListParams) {
  return useQuery({
    queryKey: ['products', 'list', params],
    queryFn: () => api.listProducts(params),
    keepPreviousData: true,
  })
}

export function useProduct(id?: string) {
  return useQuery({
    queryKey: ['products', 'detail', id],
    queryFn: () => api.getProduct(id!),
    enabled: !!id,
  })
}

export function useCreateProduct() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: api.createProduct,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['products', 'list'] })
    },
  })
}

// No generic update/delete for products in backend

export function useUploadProductImage(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (vars: { file: File; alt?: string }) => api.uploadProductImage(id, vars.file, vars.alt),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['products', 'detail', id] })
    },
  })
}

export function useSetMainImage(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (imgId: string) => api.setMainImage(id, imgId),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['products', 'detail', id] })
  })
}

export function useReorderImages(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (orderedIds: string[]) => api.reorderImages(id, orderedIds),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['products', 'detail', id] })
  })
}

export function useDeleteImage(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (imgId: string) => {
      const ok = await swalConfirm({ title: 'حذف تصویر', text: 'آیا از حذف تصویر مطمئن هستید؟', icon: 'warning', confirmText: 'بله، حذف کن', cancelText: 'خیر' })
      if (!ok) return { __cancelled: true } as any
      return api.deleteImage(id, imgId)
    },
    onSuccess: (res) => { if (!(res && (res as any).__cancelled)) qc.invalidateQueries({ queryKey: ['products', 'detail', id] }) }
  })
}

export function useAddVariant(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (vars: { value: string; sku: string; isActive: boolean }) => api.upsertVariant(id, { variantValue: vars.value, sku: vars.sku, isActive: vars.isActive }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['products', 'detail', id] })
  })
}

export function useSetVariantActive(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (vars: { value: string; sku: string; isActive: boolean }) => api.upsertVariant(id, { variantValue: vars.value, sku: vars.sku, isActive: vars.isActive }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['products', 'detail', id] })
  })
}

export function useUpdateVariant(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (vars: { variantId: string; value: string; sku: string; isActive: boolean }) =>
      api.updateVariant(id, vars.variantId, { variantValue: vars.value, sku: vars.sku, isActive: vars.isActive }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['products', 'detail', id] })
  })
}

export function useDeleteVariant(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (variantId: string) => {
      const ok = await swalConfirm({ title: 'حذف واریانت', text: 'آیا از حذف این واریانت مطمئن هستید؟', icon: 'warning', confirmText: 'بله، حذف کن', cancelText: 'خیر' })
      if (!ok) return { __cancelled: true } as any
      return api.deleteVariant(id, variantId)
    },
    onSuccess: (res) => { if (!(res && (res as any).__cancelled)) qc.invalidateQueries({ queryKey: ['products', 'detail', id] }) }
  })
}

export function useUpdateBasics(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (input: { name: string; slug: string; code: string; brandId: string; warehouseCode?: string | null; countryCode?: string | null }) => api.updateProductBasics(id, input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['products', 'detail', id] })
  })
}

export function useSetCategories(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (input: { categoryIds: string[]; primaryCategoryId?: string | null }) => api.setProductCategories(id, input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['products', 'detail', id] })
  })
}

export function useUpsertProductSeo(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (input: { storeId: string; metaTitle?: string | null; metaDescription?: string | null; canonicalUrl?: string | null; robots?: string | null; jsonLd?: string | null }) => api.upsertProductSeo(id, input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['products', 'detail', id] })
  })
}

export function useUpsertProductStore(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (input: { storeId: string; isVisible: boolean; slug: string; titleOverride?: string | null; descriptionOverride?: string | null }) => api.upsertProductStore(id, input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['products', 'detail', id] })
  })
}

export function useDeleteProductStore(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (storeId: string) => api.deleteProductStore(id, storeId),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['products', 'detail', id] })
  })
}

export function useBrands() {
  return useQuery({ queryKey: ['brands'], queryFn: api.listBrands })
}

export function useStores(search?: string) {
  return useQuery({ queryKey: ['stores', search ?? ''], queryFn: () => api.listStores(search) })
}

export function useCountries() {
  return useQuery({ queryKey: ['countries'], queryFn: api.listCountries })
}

export function useCategories() {
  return useQuery({ queryKey: ['categories', 'tree'], queryFn: api.listCategories })
}

export function useLeafCategories() {
  return useQuery({ queryKey: ['categories', 'leaves'], queryFn: api.listLeafCategories })
}

export function useLeafCategoriesWithProducts() {
  return useQuery({ queryKey: ['categories', 'leaves', 'with-products'], queryFn: api.listLeafCategoriesWithProducts })
}

export function useCreateBrand() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: api.createBrand,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['brands'] }),
  })
}

export function useBrand(id?: string) {
  return useQuery({
    queryKey: ['brands', 'detail', id],
    queryFn: () => api.getBrand(id!),
    enabled: !!id,
  })
}

export function useUpdateBrand(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (input: api.UpdateBrandInput) => api.updateBrand(id, input),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['brands'] })
      qc.invalidateQueries({ queryKey: ['brands', 'detail', id] })
    },
  })
}

export function useUploadBrandLogo(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (file: File) => api.uploadBrandLogo(id, file),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['brands'] })
      qc.invalidateQueries({ queryKey: ['brands', 'detail', id] })
    },
  })
}

export function useRenameBrand() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async ({ id, name }: { id: string; name: string }) => {
      const ok = await swalConfirm({ title: 'تغییر نام برند', text: `نام برند به «${(name||'').trim()}» تغییر یابد؟`, icon: 'question', confirmText: 'بله', cancelText: 'خیر' })
      if (!ok) return { __cancelled: true } as any
      return api.renameBrand(id, name)
    },
    onSuccess: (res) => { if (!(res && (res as any).__cancelled)) qc.invalidateQueries({ queryKey: ['brands'] }) },
  })
}

export function useDeleteBrand() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) => {
      const ok = await swalConfirm({ title: 'حذف برند', text: 'آیا از حذف برند مطمئن هستید؟', icon: 'warning', confirmText: 'بله، حذف کن', cancelText: 'خیر' })
      if (!ok) return { __cancelled: true } as any
      return api.deleteBrand(id)
    },
    onSuccess: (res) => { if (!(res && (res as any).__cancelled)) qc.invalidateQueries({ queryKey: ['brands'] }) },
  })
}

export function useCreateStore() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: api.createStore,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['stores'] }),
  })
}

export function useRenameStore() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, name }: { id: string; name: string }) => api.renameStore(id, name),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['stores'] }),
  })
}

export function useSetStoreDomain() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, domain }: { id: string; domain: string | null }) => api.setStoreDomain(id, domain),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['stores'] }),
  })
}

export function useCreateCountry() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: api.createCountry,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['countries'] }),
  })
}

export function useUpdateCountry() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ code2, input }: { code2: string; input: { nameFa: string; nameEn: string; region?: string | null; flagEmoji?: string | null } }) => api.updateCountry(code2, input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['countries'] }),
  })
}

export function useDeleteCountry() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (code2: string) => {
      const ok = await swalConfirm({ title: 'حذف کشور', text: `کشور ${code2} حذف شود؟`, icon: 'warning', confirmText: 'بله، حذف کن', cancelText: 'خیر' })
      if (!ok) return { __cancelled: true } as any
      return api.deleteCountry(code2)
    },
    onSuccess: (res) => { if (!(res && (res as any).__cancelled)) qc.invalidateQueries({ queryKey: ['countries'] }) },
  })
}

export function useCreateCategory() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: api.createCategory,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['categories', 'tree'] }),
  })
}

export function useRenameCategory() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, name, slug }: { id: string; name: string; slug: string }) => api.renameCategory(id, { name, slug }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['categories', 'tree'] }),
  })
}

export function useMoveCategory() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async ({ id, newParentId }: { id: string; newParentId: string | null }) => {
      const ok = await swalConfirm({ title: 'تایید انتقال', text: 'از انتقال دسته مطمئن هستید؟', icon: 'warning', confirmText: 'بله، منتقل کن', cancelText: 'خیر' })
      if (!ok) return { __cancelled: true } as any
      return api.moveCategory(id, newParentId)
    },
    onSuccess: (res) => { if (!(res && (res as any).__cancelled)) qc.invalidateQueries({ queryKey: ['categories', 'tree'] }) },
  })
}

export type { ListParams } from './api'
export type { ProductCreateDto } from './types'



export function useMoveCategoryNoConfirm() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, newParentId }: { id: string; newParentId: string | null }) => api.moveCategory(id, newParentId),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['categories', 'tree'] }),
  })
}

