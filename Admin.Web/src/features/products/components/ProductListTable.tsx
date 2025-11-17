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
      <table className="min-w-full text-sm">
        <thead>
          <tr className="text-left bg-gray-100">
            <th className="p-2">نام</th>
            <th className="p-2">کد</th>
            <th className="p-2">برند</th>
            <th className="p-2">وضعیت</th>
            <th className="p-2">ایجاد</th>
            <th className="p-2">عملیات</th>
          </tr>
        </thead>
        <tbody>
          {items.map((p) => (
            <tr key={p.id} className="border-b last:border-0">
              <td className="p-2 flex items-center gap-2">
                {p.mainImageUrl ? <img src={toPublicMediaUrl(p.mainImageUrl)} className="w-10 h-10 object-cover rounded" /> : null}
                <span>{p.name}</span>
              </td>
              <td className="p-2">{p.code}</td>
              <td className="p-2">{p.brandName ?? '-'}</td>
              <td className="p-2">{renderStatus(p.status)}</td>
              <td className="p-2 whitespace-nowrap">{new Date(p.createdAt).toLocaleDateString()}</td>
              <td className="p-2 flex items-center gap-2">
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
