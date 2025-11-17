import { useEffect, useState } from 'react'
import * as api from '../api'

export type VariantItem = { value: string; sku: string; isActive: boolean }

type Props = {
  value: VariantItem[]
  onChange: (items: VariantItem[]) => void
  baseCode?: string
}

export function VariantsEditor({ value, onChange, baseCode }: Props) {
  const [draft, setDraft] = useState<VariantItem>({ value: '', sku: '', isActive: true })
  const [valueSuggestions, setValueSuggestions] = useState<Array<{ value: string; sku: string; usageCount: number }>>([])
  const [recentSkus, setRecentSkus] = useState<string[]>([])

  useEffect(() => {
    api.listVariantValues(15).then(setValueSuggestions).catch(() => {})
    api.listRecentVariants(10).then(res => setRecentSkus(res.skus || [])).catch(() => {})
  }, [])

  const add = () => {
    const v = draft.value.trim()
    if (!v) return
    const nextSku = draft.sku.trim() || (baseCode ? `${baseCode}-${(value.length + 1).toString().padStart(2,'0')}` : `SKU-${Date.now()}`)
    onChange([...value, { value: v, sku: nextSku, isActive: draft.isActive }])
    setDraft({ value: '', sku: '', isActive: true })
  }
  const remove = (i: number) => onChange(value.filter((_, idx) => idx !== i))

  return (
    <div className="space-y-2">
      <div className="grid grid-cols-1 sm:grid-cols-5 gap-2">
        <input className="input sm:col-span-2" placeholder="مقدار تنوع (مثلاً: قرمز)" value={draft.value} onChange={e=>setDraft(d=>({ ...d, value: e.target.value }))} />
        <input className="input sm:col-span-2" placeholder="SKU" value={draft.sku} onChange={e=>setDraft(d=>({ ...d, sku: e.target.value }))} />
        <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={draft.isActive} onChange={e=>setDraft(d=>({ ...d, isActive: e.target.checked }))} /> فعال</label>
      </div>
      {valueSuggestions.length > 0 && (
        <div className="flex flex-wrap gap-2 text-xs">
          {valueSuggestions.map(v => (
            <button
              key={`${v.value}-${v.sku}`}
              type="button"
              className="badge badge-gray"
              onClick={() => setDraft(d => ({ ...d, value: v.value, sku: v.sku }))}
            >
              {v.value} {v.sku ? `(${v.sku})` : ''} ({v.usageCount})
            </button>
          ))}
        </div>
      )}
      {recentSkus.length > 0 && (
        <div className="flex flex-wrap gap-2 text-xs">
          {recentSkus.map(sku => (
            <button
              key={sku}
              type="button"
              className="badge badge-blue"
              onClick={() => setDraft(d => ({ ...d, sku }))}
            >
              {sku}
            </button>
          ))}
        </div>
      )}
      <button type="button" className="btn" onClick={add}>افزودن تنوع</button>
      {value.length > 0 && (
        <div className="border rounded">
          <div className="grid grid-cols-5 gap-2 px-3 py-2 bg-gray-50 text-xs">
            <div className="col-span-2">مقدار</div>
            <div className="col-span-2">SKU</div>
            <div>وضعیت</div>
          </div>
          {value.map((v, i) => (
            <div key={i} className="grid grid-cols-5 gap-2 px-3 py-2 border-b last:border-0 text-sm items-center">
              <div className="col-span-2">{v.value}</div>
              <div className="col-span-2 font-mono">{v.sku}</div>
              <div className="flex items-center gap-2">
                <span>{v.isActive ? 'فعال' : 'غیرفعال'}</span>
                <button type="button" className="btn-secondary px-2 py-1 rounded" onClick={() => remove(i)}>حذف</button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
