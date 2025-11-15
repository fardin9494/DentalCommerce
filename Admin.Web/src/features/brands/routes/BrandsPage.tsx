import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { PageHeader } from '../../../shared/components/PageHeader'
import { Spinner } from '../../../shared/components/Spinner'
import { useBrands, useCreateBrand, useDeleteBrand } from '../../products/queries'
import { swalToastSuccess, swalToastError } from '../../../shared/utils/swal'
import type { BrandStatusValue } from '../../products/api'

const brandStatusOptions = [
  { value: '1', label: 'Active' },
  { value: '2', label: 'Inactive' },
  { value: '3', label: 'Deprecated' },
] as const

type BrandStatusOptionValue = (typeof brandStatusOptions)[number]['value']

type BrandFormState = {
  name: string
  website: string
  description: string
  establishedYear: string
  status: BrandStatusOptionValue
}

const makeInitialForm = (): BrandFormState => ({
  name: '',
  website: '',
  description: '',
  establishedYear: '',
  status: brandStatusOptions[0].value,
})

export function BrandsPage() {
  const { data: brands, isLoading } = useBrands()
  const create = useCreateBrand()
  const del = useDeleteBrand()
  const navigate = useNavigate()
  const [form, setForm] = useState<BrandFormState>(() => makeInitialForm())

  return (
    <div className="space-y-4">
      <PageHeader title="Brands">Brands</PageHeader>
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        <div className="lg:col-span-2 card p-4">
          {isLoading ? <Spinner /> : (
            <div className="overflow-x-auto">
              <table className="min-w-full text-sm">
                <thead>
                  <tr className="bg-gray-100 text-left">
                    <th className="p-2">Name</th>
                    <th className="p-2">Status</th>
                    <th className="p-2">Products</th>
                    <th className="p-2">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {brands?.map(b => (
                    <tr key={b.id} className="border-b last:border-0">
                      <td className="p-2">{b.name}</td>
                      <td className="p-2">{String(b.status)}</td>
                      <td className="p-2">{b.productsCount}</td>
                      <td className="p-2 flex gap-2">
                        <button className="btn-secondary px-3 py-1.5 rounded" onClick={() => navigate(`/brands/${b.id}`)}>Edit</button>
                        <button className="btn-secondary px-3 py-1.5 rounded" onClick={async () => {
                          try {
                            await del.mutateAsync(b.id)
                            swalToastSuccess('Brand deleted')
                          } catch (e:any) {
                            swalToastError(e?.message || 'Delete failed')
                          }
                        }}>Delete</button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
        <div className="card p-4">
          <h3 className="font-semibold mb-3">Create brand</h3>
          <form className="space-y-3" onSubmit={async (e) => {
            e.preventDefault()
            const rawYear = form.establishedYear.trim()
            let establishedYear: number | null = null
            if (rawYear) {
              const parsedYear = Number.parseInt(rawYear, 10)
              if (Number.isNaN(parsedYear)) {
                swalToastError('Established year is invalid')
                return
              }
              establishedYear = parsedYear
            }
            const payload = {
              name: form.name.trim(),
              website: form.website.trim() || null,
              description: form.description.trim() || null,
              establishedYear,
              status: Number(form.status) as BrandStatusValue,
            }
            try {
              await create.mutateAsync(payload)
              setForm(makeInitialForm())
              swalToastSuccess('Brand created successfully')
            } catch (err:any) {
              swalToastError(err?.message || 'Failed to create brand')
            }
          }}>
            <div>
              <label className="label">Name</label>
              <input className="input" value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} />
            </div>
            <div>
              <label className="label">Website</label>
              <input className="input" value={form.website} onChange={e => setForm(f => ({ ...f, website: e.target.value }))} />
            </div>
            <div>
              <label className="label">Description</label>
              <textarea className="input min-h-[96px]" value={form.description} onChange={e => setForm(f => ({ ...f, description: e.target.value }))} />
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <div>
                <label className="label">Established year</label>
                <input className="input" type="number" min="1800" max="3000" value={form.establishedYear} onChange={e => setForm(f => ({ ...f, establishedYear: e.target.value }))} />
              </div>
            </div>
            <div>
              <label className="label">Status</label>
              <select className="input" value={form.status} onChange={e => setForm(f => ({ ...f, status: e.target.value as BrandStatusOptionValue }))}>
                {brandStatusOptions.map(opt => (
                  <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
              </select>
            </div>
            <button type="submit" className="btn w-full">Create</button>
          </form>
        </div>
      </div>
    </div>
  )
}
