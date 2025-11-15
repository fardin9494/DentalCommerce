import { useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useNavigate } from 'react-router-dom'
import { PageHeader } from '../../../shared/components/PageHeader'
import { ProductCreateSchema, type ProductCreateDto } from '../types'
import { BrandSelect } from '../components/BrandSelect'
import { CategoryMultiSelect } from '../components/CategoryMultiSelect'
import { PropertiesEditor, type PropertyItem } from '../components/PropertiesEditor'
import { VariantsEditor, type VariantItem } from '../components/VariantsEditor'
import { DescriptionEditor } from '../components/DescriptionEditor'
import { swalToastError, swalToastSuccess } from '../../../shared/utils/swal'
import { useCountries } from '../queries'
import * as api from '../api'

export function CreateProductPage() {
  const navigate = useNavigate()
  const [brandId, setBrandId] = useState<string | undefined>(undefined)
  const [categoryIds, setCategoryIds] = useState<string[]>([])
  const [propsList, setPropsList] = useState<PropertyItem[]>([])
  const [descHtml, setDescHtml] = useState<string>('')
  const [hasVariation, setHasVariation] = useState<boolean>(false)
  const [variants, setVariants] = useState<VariantItem[]>([])
  const { data: countries } = useCountries()

  const { register, handleSubmit, setValue, watch, formState: { errors, isSubmitting } } = useForm<ProductCreateDto>({
    resolver: zodResolver(ProductCreateSchema),
    defaultValues: { name: '', slug: '', code: '', brandId: '', categoryIds: [], warehouseCode: '', variationKey: '', countryCode: null },
  })

  const nameValue = watch('name')
  const slugValue = watch('slug')
  const toSlug = (s: string) =>
    s
      .trim()
      .toLowerCase()
      // keep Persian letters, English letters/numbers and space/hyphen
      .replace(/[^\u0600-\u06FFa-z0-9\s-]/g, '')
      .replace(/\s+/g, '-')
      .replace(/-+/g, '-')
  const canGenerateSlug = useMemo(() => (nameValue || '').trim().length > 0, [nameValue])

  const onSubmit = async (v: ProductCreateDto) => {
    let newId = ''
    try {
      const payload = { ...v, brandId: brandId!, categoryIds }
      if (hasVariation && !(v.variationKey && v.variationKey.trim())) {
        swalToastError('کلید واریانت اجباری است (Variation Key)')
        return
      }
      const { id } = await api.createProduct(payload)
      newId = id

      // Best-effort follow-up steps; do not block navigation
      try {
        if (hasVariation) {
          await api.setVariation(id, (v.variationKey && v.variationKey.trim()) || null)
          for (const it of variants) {
            if (!it.value.trim()) continue
            await api.upsertVariant(id, { variantValue: it.value.trim(), sku: it.sku || `${v.code}-${it.value}`, isActive: it.isActive })
          }
        } else if (v.variationKey) {
          await api.setVariation(id, null)
        }
      } catch { /* ignore */ }

      try {
        if (descHtml && descHtml.trim()) {
          await api.setProductDescription(id, descHtml)
        }
      } catch { /* ignore */ }

      try {
        if (propsList.length) {
          for (const p of propsList) {
            if (p.key.trim()) await api.upsertProductProperty(id, { key: p.key.trim(), valueString: p.value })
          }
        }
      } catch { /* ignore */ }

      swalToastSuccess('محصول با موفقیت ایجاد شد')
    } catch (e:any) {
      const msg = e?.message || 'خطا در ایجاد محصول'
      swalToastError(msg)
    } finally {
      if (newId) navigate(`/products/${newId}`)
    }
  }

  return (
    <div className="space-y-4">
      <PageHeader title="ایجاد محصول" />
      <form onSubmit={handleSubmit(onSubmit)} className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        <div className="lg:col-span-2 card p-4 space-y-3">
          <h3 className="font-semibold">اطلاعات پایه</h3>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div>
              <label className="label">نام</label>
              <input className="input" {...register('name')} />
              {errors.name && <p className="text-red-600 text-xs mt-1">{String(errors.name.message)}</p>}
            </div>
            <div>
              <label className="label flex items-center justify-between">
                <span>اسلاگ</span>
                <button type="button" className="btn btn-xs" disabled={!canGenerateSlug} onClick={() => setValue('slug', toSlug(nameValue || ''))}>تبدیل از نام</button>
              </label>
              <input className="input" placeholder="مثال: product-name" {...register('slug')} value={slugValue || ''} onChange={(e) => setValue('slug', e.target.value)} />
              {errors.slug && <p className="text-red-600 text-xs mt-1">{String(errors.slug.message)}</p>}
              <p className="text-gray-500 text-xs mt-1">از حروف فارسی/انگلیسی، اعداد و خط تیره استفاده کنید.</p>
            </div>
          </div>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div>
              <label className="label">کد کالا</label>
              <input className="input" {...register('code')} />
              {errors.code && <p className="text-red-600 text-xs mt-1">{String(errors.code.message)}</p>}
            </div>
            <div>
              <label className="label">کد انبار</label>
              <input className="input" {...register('warehouseCode')} />
              <p className="text-gray-500 text-xs mt-1">اختیاری</p>
            </div>
          </div>

          <h3 className="font-semibold mt-2">برند و دسته‌بندی</h3>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div>
              <BrandSelect value={brandId} onChange={(id) => { setBrandId(id); setValue('brandId', id ?? '') }} />
            </div>
            <div>
              <CategoryMultiSelect value={categoryIds} onChange={(ids) => { setCategoryIds(ids); setValue('categoryIds', ids) }} />
              <div className="text-xs text-gray-500 mt-1">{categoryIds.length} دسته انتخاب شده</div>
            </div>
          </div>

          <h3 className="font-semibold mt-2">کشور سازنده</h3>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div>
              <label className="label">کشور سازنده</label>
              <select className="input" value={watch('countryCode') ?? ''} onChange={(e) => setValue('countryCode', e.target.value ? e.target.value : null)}>
                <option value="">- انتخاب کشور -</option>
                {(countries || []).map((c) => (
                  <option key={c.code2} value={c.code2}>{c.nameFa || c.nameEn}</option>
                ))}
              </select>
              <p className="text-gray-500 text-xs mt-1">در صورت نامشخص بودن، خالی بگذارید.</p>
            </div>
          </div>

          <div className="space-y-2">
            <label className="flex items-center gap-2 text-sm">
              <input type="checkbox" checked={hasVariation} onChange={e=>setHasVariation(e.target.checked)} />
              این محصول واریانت دارد
            </label>
            {hasVariation && (
              <>
                <div>
                  <label className="label">Variation Key (کلید واریانت)</label>
                  <input className="input" {...register('variationKey')} />
                  {errors.variationKey && <p className="text-red-600 text-xs mt-1">{String(errors.variationKey.message)}</p>}
                  <p className="text-gray-500 text-xs mt-1">مثال: رنگ، سایز</p>
                </div>
                <div className="space-y-2">
                  <div className="font-semibold">لیست واریانت‌ها</div>
                  <VariantsEditor value={variants} onChange={setVariants} baseCode={watch('code')} />
                </div>
              </>
            )}
          </div>

          <div className="space-y-2">
            <div className="font-semibold">مشخصات (Properties)</div>
            <PropertiesEditor value={propsList} onChange={setPropsList} />
          </div>

          <DescriptionEditor value={descHtml} onChange={setDescHtml} />

          <div className="pt-2 flex items-center gap-2">
            <button disabled={isSubmitting} className="btn" type="submit">{isSubmitting ? 'در حال ایجاد...' : 'ایجاد محصول'}</button>
            <span className="text-xs text-gray-500">پس از ایجاد، به صفحه محصول هدایت می‌شوید.</span>
          </div>
        </div>
        <div className="card p-4 text-sm text-gray-600">
          <div className="font-semibold mb-2">راهنما</div>
          <ul className="list-disc pr-5 space-y-1">
            <li>انتخاب برند و دسته‌بندی الزامی است.</li>
            <li>در صورت داشتن واریانت، وارد کردن Variation Key لازم است.</li>
            <li>پس از ایجاد، می‌توانید تصاویر را بارگذاری کنید.</li>
            <li>می‌توانید اسلاگ را با دکمه «تبدیل از نام» بسازید.</li>
          </ul>
        </div>
      </form>
      
    </div>
  )
}
