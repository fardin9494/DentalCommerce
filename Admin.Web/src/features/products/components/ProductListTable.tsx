import { Link } from 'react-router-dom'
import * as api from '../api'
import { useQueryClient } from '@tanstack/react-query'
import { toPublicMediaUrl } from '../../../lib/api/client'
import type { ProductListItem } from '../types'
import { formatJalaliDate } from '../../../shared/utils/date'

type Props = {
  items: ProductListItem[]
}

export function ProductListTable({ items }: Props) {
  const qc = useQueryClient()
  return (
    <div className="overflow-x-auto">
      <table className="min-w-full text-sm text-center">
        <thead>
          <tr className="bg-gray-100">
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
              <td className="p-2">
                <div className="flex flex-col items-center gap-1">
                  {p.mainImageUrl ? <img src={toPublicMediaUrl(p.mainImageUrl)} className="w-10 h-10 object-cover rounded" /> : null}
                  <span>{truncateName(p.name)}</span>
                </div>
              </td>
              <td className="p-2">{p.code}</td>
              <td className="p-2">{p.brandName ?? '-'}</td>
              <td className="p-2">{renderStatus(p.status)}</td>
              <td className="p-2 whitespace-nowrap">{formatJalaliDate(p.createdAt)}</td>
              <td className="p-2">
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

function truncateName(name: string, maxLength = 30) {
  if (!name) return ''
  if (name.length <= maxLength) return name
  return `${name.slice(0, maxLength)}…`
}

function renderStatus(s: string) {
  if (s === 'Active') return <span className="badge badge-green">فعال</span>
  if (s === 'Hidden') return <span className="badge badge-red">مخفی</span>
  return <span className="badge badge-gray">پیش‌نویس</span>
}
