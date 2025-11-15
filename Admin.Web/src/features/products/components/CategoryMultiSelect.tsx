import { useMemo, useState } from 'react'
import { useLeafCategories } from '../queries'

type Props = {
  value: string[]
  onChange: (ids: string[]) => void
  primaryId?: string
  onSetPrimary?: (id: string) => void
}

export function CategoryMultiSelect({ value, onChange, primaryId, onSetPrimary }: Props) {
  const { data } = useLeafCategories()
  const [q, setQ] = useState('')

  const options = useMemo(() => {
    const all = data ?? []
    if (!q.trim()) return all
    const s = q.toLowerCase()
    return all.filter(c => c.name.toLowerCase().includes(s) || c.slug.toLowerCase().includes(s))
  }, [data, q])

  const toggle = (id: string) => {
    if (value.includes(id)) onChange(value.filter(v => v !== id))
    else onChange([...value, id])
  }

  return (
    <div className="space-y-2">
      <div>
        <div className="label mb-1">دسته‌بندی‌ها</div>
        <input className="input" placeholder="جستجوی دسته‌بندی" value={q} onChange={e=>setQ(e.target.value)} />
      </div>
      <ul className="border rounded max-h-72 overflow-y-auto text-sm divide-y">
        {(options ?? []).map(c => {
          const selected = value.includes(c.id)
          const isPrimary = !!primaryId && primaryId === c.id
          return (
            <li key={c.id} className="px-3 py-2 flex items-center justify-between hover:bg-gray-50 group">
              <div className="flex items-center gap-2">
                <input type="checkbox" checked={selected} onChange={()=>toggle(c.id)} />
                <span className="text-gray-400">{'›'.repeat(Math.max(0,(c.depth??1)-1))}</span>
                <span className={isPrimary ? 'font-medium' : ''}>{c.name}</span>
                {isPrimary && <span className="text-xs text-gray-500">(Primary)</span>}
              </div>
              {onSetPrimary && (
                <button
                  type="button"
                  className="opacity-0 group-hover:opacity-100 text-xs underline px-2 py-1"
                  onClick={()=>onSetPrimary(c.id)}
                  disabled={!selected}
                  title={selected ? 'انتخاب بعنوان اصلی' : 'ابتدا دسته را انتخاب کنید'}
                >
                  انتخاب بعنوان اصلی
                </button>
              )}
            </li>
          )
        })}
        {!options?.length && <li className="px-3 py-2 text-gray-500">موردی یافت نشد</li>}
      </ul>
    </div>
  )
}
