import { useState } from 'react'
import { PageHeader } from '../../../shared/components/PageHeader'
import { Spinner } from '../../../shared/components/Spinner'
import { useStores, useCreateStore, useRenameStore, useSetStoreDomain } from '../../products/queries'
import { swalPrompt, swalToastSuccess, swalToastError } from '../../../shared/utils/swal'

export function StoresPage() {
  const { data: stores, isLoading, isError, error } = useStores()
  const create = useCreateStore()
  const rename = useRenameStore()
  const setDomain = useSetStoreDomain()
  const [form, setForm] = useState({ name: '', domain: '' })

  return (
    <div className="space-y-4">
      <PageHeader title="Stores">مدیریت فروشگاه‌ها</PageHeader>
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        <div className="lg:col-span-2 card p-4">
          {isLoading ? <Spinner /> : isError ? (
            <div className="text-sm text-red-600">Failed to load stores{(error as any)?.message ? `: ${(error as any).message}` : ''}</div>
          ) : (
            <div className="overflow-x-auto">
              <table className="min-w-full text-sm">
                <thead>
                  <tr className="bg-gray-100 text-left">
                    <th className="p-2">Name</th>
                    <th className="p-2">Domain</th>
                    <th className="p-2">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {stores?.map(s => (
                    <tr key={s.id} className="border-b last:border-0">
                      <td className="p-2">{s.name}</td>
                      <td className="p-2">{s.domain || '-'}</td>
                      <td className="p-2 flex gap-2">
                        <button className="btn-secondary px-3 py-1.5 rounded" onClick={async () => {
                          const name = await swalPrompt({ title: 'ویرایش نام فروشگاه', defaultValue: s.name, required: true, confirmText: 'ذخیره', cancelText: 'لغو' })
                          if (!name) return
                          try { await rename.mutateAsync({ id: s.id, name }); swalToastSuccess('نام فروشگاه بروزرسانی شد') } catch (e:any) { swalToastError(e?.message || 'خطا در تغییر نام فروشگاه') }
                        }}>Rename</button>
                        <button className="btn-secondary px-3 py-1.5 rounded" onClick={async () => {
                          const domain = await swalPrompt({ title: 'دامنه فروشگاه (اختیاری)', defaultValue: s.domain || '', confirmText: 'ذخیره', cancelText: 'لغو' })
                          if (domain === null) return
                          try { await setDomain.mutateAsync({ id: s.id, domain: (domain || '').trim() || null }); swalToastSuccess('دامنه بروزرسانی شد') } catch (e:any) { swalToastError(e?.message || 'خطا در ثبت دامنه') }
                        }}>Set Domain</button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
        <div className="card p-4">
          <h3 className="font-semibold mb-3">ایجاد فروشگاه</h3>
          <form className="space-y-3" onSubmit={async (e) => {
            e.preventDefault();
            try {
              await create.mutateAsync({ name: form.name, domain: form.domain || null })
              setForm({ name: '', domain: '' })
              swalToastSuccess('فروشگاه ایجاد شد')
            } catch (err:any) {
              swalToastError(err?.message || 'ایجاد فروشگاه ناموفق بود')
            }
          }}>
            <div>
              <label className="label">Name</label>
              <input className="input" value={form.name} onChange={e=>setForm(f=>({ ...f, name: e.target.value }))} />
            </div>
            <div>
              <label className="label">Domain (optional)</label>
              <input className="input" value={form.domain} onChange={e=>setForm(f=>({ ...f, domain: e.target.value }))} />
            </div>
            <button type="submit" className="btn">Create</button>
          </form>
        </div>
      </div>
    </div>
  )
}

