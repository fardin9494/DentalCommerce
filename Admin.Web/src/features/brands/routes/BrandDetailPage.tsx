import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { PageHeader } from '../../../shared/components/PageHeader'
import { Spinner } from '../../../shared/components/Spinner'
import { useBrand, useUpdateBrand, useUploadBrandLogo } from '../../products/queries'
import { swalToastError, swalToastSuccess } from '../../../shared/utils/swal'
import type { BrandStatusValue } from '../../products/api'
import { toPublicMediaUrl } from '../../../lib/api/client'

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

const defaultState = (): BrandFormState => ({
  name: '',
  website: '',
  description: '',
  establishedYear: '',
  status: brandStatusOptions[0].value,
})

export function BrandDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { data: brand, isLoading } = useBrand(id)
  const update = useUpdateBrand(id || '')
  const uploadLogo = useUploadBrandLogo(id || '')
  const [form, setForm] = useState<BrandFormState>(() => defaultState())

  useEffect(() => {
    if (!brand) return
    setForm({
      name: brand.name,
      website: brand.website ?? '',
      description: brand.description ?? '',
      establishedYear: brand.establishedYear ? String(brand.establishedYear) : '',
      status: String(brand.status ?? 1) as BrandStatusOptionValue,
    })
  }, [brand])

  const logoUrl = useMemo(() => toPublicMediaUrl(brand?.logoUrl), [brand?.logoUrl])

  if (!id) {
    return (
      <div className="space-y-4">
        <PageHeader title="Brand details">
          <span className="text-red-600">Invalid brand id</span>
        </PageHeader>
      </div>
    )
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!brand) return
    const trimmedName = form.name.trim()
    if (!trimmedName) {
      swalToastError('Name is required')
      return
    }
    const rawYear = form.establishedYear.trim()
    let establishedYear: number | null = null
    if (rawYear) {
      const parsed = Number.parseInt(rawYear, 10)
      if (Number.isNaN(parsed)) {
        swalToastError('Established year is invalid')
        return
      }
      establishedYear = parsed
    }
    try {
      await update.mutateAsync({
        name: trimmedName,
        website: form.website.trim() || null,
        description: form.description.trim() || null,
        establishedYear,
        status: Number(form.status) as BrandStatusValue,
      })
      swalToastSuccess('Brand updated')
    } catch (err: any) {
      swalToastError(err?.message || 'Failed to update brand')
    }
  }

  const handleLogoChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    if (!brand) return
    const file = e.target.files?.[0]
    if (!file) return
    try {
      await uploadLogo.mutateAsync(file)
      swalToastSuccess('Logo updated')
    } catch (err: any) {
      swalToastError(err?.message || 'Logo upload failed')
    } finally {
      e.target.value = ''
    }
  }

  return (
    <div className="space-y-4">
      <PageHeader
        title={brand ? `Edit brand: ${brand.name}` : 'Edit brand'}
        actions={<Link to="/brands" className="btn-secondary px-4 py-2 rounded">Back to list</Link>}
      />

      {isLoading && (
        <div className="card p-6 flex items-center justify-center">
          <Spinner />
        </div>
      )}

      {!isLoading && !brand && (
        <div className="card p-6 text-sm text-red-600">Brand not found.</div>
      )}

      {brand && (
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
          <div className="lg:col-span-2 card p-4 space-y-4">
            <h2 className="font-semibold text-base">Basic information</h2>
            <form className="space-y-3" onSubmit={handleSubmit}>
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
                <textarea className="input min-h-[120px]" value={form.description} onChange={e => setForm(f => ({ ...f, description: e.target.value }))} />
              </div>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                <div>
                  <label className="label">Established year</label>
                  <input className="input" type="number" min="1800" max="3000" value={form.establishedYear} onChange={e => setForm(f => ({ ...f, establishedYear: e.target.value }))} />
                </div>
                <div>
                  <label className="label">Status</label>
                  <select className="input" value={form.status} onChange={e => setForm(f => ({ ...f, status: e.target.value as BrandStatusOptionValue }))}>
                    {brandStatusOptions.map(opt => (
                      <option key={opt.value} value={opt.value}>{opt.label}</option>
                    ))}
                  </select>
                </div>
              </div>
              <div className="flex gap-2">
                <button type="submit" className="btn px-4 py-2" disabled={update.isPending}>
                  {update.isPending ? 'Saving...' : 'Save changes'}
                </button>
                <button type="button" className="btn-secondary px-4 py-2 rounded" onClick={() => navigate('/brands')}>
                  Cancel
                </button>
              </div>
            </form>
          </div>
          <div className="card p-4 space-y-3">
            <h2 className="font-semibold text-base">Logo</h2>
            {logoUrl ? (
              <img src={logoUrl} alt={`${brand.name} logo`} className="w-48 h-48 object-contain border rounded bg-white" />
            ) : (
              <div className="w-48 h-48 flex items-center justify-center border rounded bg-gray-50 text-xs text-gray-500">
                No logo uploaded
              </div>
            )}
            <div>
              <label className="label">Upload new logo</label>
              <input type="file" accept="image/*" onChange={handleLogoChange} disabled={uploadLogo.isPending} />
            </div>
            {uploadLogo.isPending && (
              <div className="text-xs text-gray-500 flex items-center gap-2">
                <Spinner /> <span>Uploading...</span>
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  )
}
