import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { ProductCreateSchema, type ProductCreateDto } from '../types'
import { useBrands, useCategories } from '../queries'

type Props = {
  defaultValues?: Partial<ProductCreateDto>
  onSubmit: (values: ProductCreateDto) => void | Promise<void>
  submitText?: string
}

export function ProductForm({ defaultValues, onSubmit, submitText = 'ذخیره' }: Props) {
  const { data: brands } = useBrands()
  const { data: categories } = useCategories()
  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<ProductCreateDto>({
    resolver: zodResolver(ProductCreateSchema),
    defaultValues: {
      name: '', slug: '', code: '', brandId: '', categoryIds: [], warehouseCode: '', variationKey: '', ...defaultValues,
    },
  })

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-3">
      <div>
        <label className="label">نام</label>
        <input className="input" {...register('name')} />
        {errors.name && <p className="text-red-600 text-xs mt-1">{errors.name.message}</p>}
      </div>
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <div>
          <label className="label">Slug</label>
          <input className="input" {...register('slug')} />
          {errors.slug && <p className="text-red-600 text-xs mt-1">{errors.slug.message}</p>}
        </div>
        <div>
          <label className="label">کد</label>
          <input className="input" {...register('code')} />
          {errors.code && <p className="text-red-600 text-xs mt-1">{errors.code.message}</p>}
        </div>
      </div>
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <div>
          <label className="label">برند</label>
          <select className="input" {...register('brandId')}>
            <option value="">-</option>
            {brands?.map(b => <option key={b.id} value={b.id}>{b.name}</option>)}
          </select>
        </div>
        <div>
          <label className="label">دسته‌ها</label>
          <select multiple className="input h-28" {...register('categoryIds') as any}>
            {categories?.map(c => <option key={c.id} value={c.id}>{`${'—'.repeat(Math.max(0,c.depth-1))} ${c.name}`}</option>)}
          </select>
        </div>
      </div>
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <div>
          <label className="label">کد انبار</label>
          <input className="input" {...register('warehouseCode')} />
        </div>
        <div>
          <label className="label">Variation Key</label>
          <input className="input" {...register('variationKey')} />
        </div>
      </div>
      <div className="pt-2">
        <button disabled={isSubmitting} className="btn" type="submit">{submitText}</button>
      </div>
    </form>
  )
}
