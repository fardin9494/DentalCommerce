import { Link } from 'react-router-dom'
import * as api from '../api'
import { useQueryClient } from '@tanstack/react-query'
import { toPublicMediaUrl } from '../../../lib/api/client'
import type { ProductListItem } from '../types'

type Props = {
  items: ProductListItem[]
}

export function ProductListTable({ items }: Props) {
  const qc = useQueryClient()
  return (
    <div className="overflow-x-auto">
      <table className="min-w-full text-sm table-fixed">
        <colgroup>
          <col className="w-4/12" />
          <col className="w-2/12" />
          <col className="w-2/12" />
          <col className="w-1/12" />
          <col className="w-2/12" />
          <col className="w-1/12" />
        </colgroup>
        <thead>
          <tr className="text-left bg-gray-100">
            <th className="p-2 align-top">نام</th>
            <th className="p-2 align-top">کد</th>
            <th className="p-2 align-top">برند</th>
            <th className="p-2 align-top">وضعیت</th>
            <th className="p-2 align-top">ایجاد</th>
            <th className="p-2 align-top">عملیات</th>
          </tr>
        </thead>
        <tbody>
          {items.map((p) => (
            <tr key={p.id} className="border-b last:border-0">
              <td className="p-2 align-top">
                <div className="flex items-start gap-2">
                  {p.mainImageUrl ? <img src={toPublicMediaUrl(p.mainImageUrl)} className="w-10 h-10 object-cover rounded" /> : null}
                  <span>{p.name}</span>
                </div>
              </td>
              <td className="p-2 align-top">{p.code}</td>
              <td className="p-2 align-top">{p.brandName ?? '-'}</td>
              <td className="p-2 align-top">{renderStatus(p.status)}</td>
              <td className="p-2 align-top whitespace-nowrap">{new Date(p.createdAt).toLocaleDateString()}</td>
              <td className="p-2 align-top">
                <div className="flex flex-wrap gap-2">
                  <Link to={`/products/${p.id}`} className="btn">جزئیات</Link>
                  {p.status === 'Active' ? (
                    <button className="btn-red" onClick={async ()=>{
                      try { await api.hideProduct(p.id); await qc.invalidateQueries({ queryKey: ['products','list'] }); alert('مخفی شد') } catch(e:any){ alert(e?.message || 'خطا در مخفی‌سازی') }
                    }}>مخفی کردن</button>
                  ) : (
                    <button className="btn-green" onClick={async ()=>{
                      try { await api.activateProduct(p.id); await qc.invalidateQueries({ queryKey: ['products','list'] }); alert('فعال شد') } catch(e:any){ alert(e?.message || 'خطا در فعال‌سازی') }
                    }}>فعال‌سازی</button>
                  )}
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

function renderStatus(s: string) {
  if (s === 'Active') return <span className="badge badge-green">فعال</span>
  if (s === 'Hidden') return <span className="badge badge-red">مخفی</span>
  return <span className="badge badge-gray">پیش‌نویس</span>
}

