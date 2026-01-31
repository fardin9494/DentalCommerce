import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { PageHeader } from '../../../shared/components/PageHeader'
import { Spinner } from '../../../shared/components/Spinner'
import { formatJalaliDate } from '../../../shared/utils/date'
import { swalConfirm, swalToastError, swalToastSuccess } from '../../../shared/utils/swal'
import { JalaliDateTimePicker } from '../../../shared/components/JalaliDateTimePicker'
import { getApiErrorMessage } from '../api'
import { useCreatePriceList, useDeletePriceList, usePriceLists } from '../queries'

type FormState = {
  name: string
  currency: string
  validFrom: string
  validTo: string
  isActive: boolean
}

const emptyForm = (): FormState => ({
  name: '',
  currency: 'IRR',
  validFrom: '',
  validTo: '',
  isActive: true,
})

export function PriceListsPage() {
  const navigate = useNavigate()
  const { data, isLoading } = usePriceLists()
  const create = useCreatePriceList()
  const del = useDeletePriceList()
  const [form, setForm] = useState<FormState>(() => emptyForm())

  const handleCreate = async (e: FormEvent) => {
    e.preventDefault()
    if (!form.name.trim()) {
      swalToastError('نام لیست قیمت لازم است.')
      return
    }
    if (!form.currency.trim()) {
      swalToastError('ارز را مشخص کنید.')
      return
    }
    try {
      await create.mutateAsync({
        name: form.name.trim(),
        currency: form.currency.trim(),
        validFrom: form.validFrom || null,
        validTo: form.validTo || null,
        isActive: form.isActive,
      })
      setForm(emptyForm())
      swalToastSuccess('لیست قیمت ایجاد شد.')
    } catch (err) {
      swalToastError(getApiErrorMessage(err, 'خطا در ایجاد لیست قیمت.'))
    }
  }

  const handleDelete = async (id: string) => {
    const ok = await swalConfirm({
      title: 'حذف لیست قیمت',
      text: 'با حذف این لیست قیمت ممکن است داده‌های وابسته از بین بروند.',
      icon: 'warning',
      confirmText: 'بله',
      cancelText: 'انصراف',
    })
    if (!ok) return
    try {
      await del.mutateAsync(id)
      swalToastSuccess('لیست قیمت حذف شد.')
    } catch (err) {
      swalToastError(getApiErrorMessage(err, 'خطا در حذف لیست قیمت.'))
    }
  }

  return (
    <div className="space-y-4">
      <PageHeader title="لیست قیمت‌ها">
        مدیریت لیست‌های قیمت و بازه‌های اعتبار هر لیست.
      </PageHeader>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        <div className="lg:col-span-2 card p-4">
          {isLoading ? <Spinner /> : (
            <div className="overflow-x-auto">
              <table className="min-w-full text-sm text-center">
                <thead>
                  <tr className="bg-gray-100">
                    <th className="p-2">نام</th>
                    <th className="p-2">ارز</th>
                    <th className="p-2">اعتبار از</th>
                    <th className="p-2">اعتبار تا</th>
                    <th className="p-2">وضعیت</th>
                    <th className="p-2">تعداد آیتم‌ها</th>
                    <th className="p-2">عملیات</th>
                  </tr>
                </thead>
                <tbody>
                  {(data ?? []).length === 0 && (
                    <tr className="border-b last:border-0">
                      <td colSpan={7} className="p-3 text-sm text-gray-600">لیست قیمتی ثبت نشده است.</td>
                    </tr>
                  )}
                  {data?.map(list => (
                    <tr key={list.id} className="border-b last:border-0">
                      <td className="p-2">{list.name}</td>
                      <td className="p-2">{list.currency}</td>
                      <td className="p-2">{formatJalaliDate(list.validFrom)}</td>
                      <td className="p-2">{formatJalaliDate(list.validTo)}</td>
                      <td className="p-2">
                        {list.isActive ? <span className="badge badge-green">فعال</span> : <span className="badge badge-gray">غیرفعال</span>}
                      </td>
                      <td className="p-2">{list.items?.length ?? 0}</td>
                      <td className="p-2">
                        <div className="flex items-center justify-center gap-2">
                          <button className="btn-secondary px-3 py-1.5 rounded" onClick={() => navigate(`/pricing/pricelists/${list.id}`)}>
                            مدیریت
                          </button>
                          <button className="btn-red px-3 py-1.5 rounded" onClick={() => handleDelete(list.id)}>
                            حذف
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>

        <div className="card p-4">
          <h3 className="font-semibold mb-3">ایجاد لیست قیمت</h3>
          <form className="space-y-3" onSubmit={handleCreate}>
            <div>
              <label className="label">نام</label>
              <input className="input" value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} />
            </div>
            <div>
              <label className="label">ارز</label>
              <input className="input" value={form.currency} onChange={e => setForm(f => ({ ...f, currency: e.target.value }))} />
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <div>
                <label className="label">اعتبار از</label>
                <JalaliDateTimePicker value={form.validFrom} onChange={(v) => setForm(f => ({ ...f, validFrom: v }))} clearable />
              </div>
              <div>
                <label className="label">اعتبار تا</label>
                <JalaliDateTimePicker value={form.validTo} onChange={(v) => setForm(f => ({ ...f, validTo: v }))} clearable />
              </div>
            </div>
            <label className="flex items-center gap-2 text-sm">
              <input type="checkbox" checked={form.isActive} onChange={e => setForm(f => ({ ...f, isActive: e.target.checked }))} />
              فعال باشد
            </label>
            <button type="submit" className="btn w-full" disabled={create.isPending}>
              {create.isPending ? 'در حال ایجاد...' : 'ایجاد'}
            </button>
          </form>
        </div>
      </div>
    </div>
  )
}
