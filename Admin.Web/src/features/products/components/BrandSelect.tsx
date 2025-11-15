import { useEffect, useMemo, useState } from 'react'
import { useBrands } from '../queries'

type Props = {
  value?: string
  onChange: (id: string | undefined) => void
}

export function BrandSelect({ value, onChange }: Props) {
  const { data } = useBrands()
  const [open, setOpen] = useState(false)
  const [q, setQ] = useState('')

  const options = useMemo(() => {
    const all = data ?? []
    if (!q.trim()) return all
    const s = q.toLowerCase()
    return all.filter(b => b.name.toLowerCase().includes(s))
  }, [data, q])

  const selected = useMemo(() => data?.find(b => b.id === value), [data, value])

  useEffect(() => {
    const close = () => setOpen(false)
    window.addEventListener('click', close)
    return () => window.removeEventListener('click', close)
  }, [])

  return (
    <div className="relative" onClick={e => e.stopPropagation()}>
      <div className="label mb-1">برند</div>
      <button type="button" className="input text-right flex items-center justify-between" onClick={() => setOpen(o=>!o)}>
        <span>{selected ? selected.name : 'انتخاب برند'}</span>
        <span className="text-xs text-gray-500">▼</span>
      </button>
      {open && (
        <div className="absolute z-10 mt-1 w-full bg-white border rounded shadow">
          <div className="p-2">
            <input className="input" placeholder="جستجوی برند" value={q} onChange={e=>setQ(e.target.value)} />
          </div>
          <ul className="max-h-60 overflow-y-auto text-sm">
            {(options ?? []).map(b => (
              <li key={b.id} className={`px-3 py-2 hover:bg-gray-100 cursor-pointer ${b.id === value ? 'bg-gray-50' : ''}`} onClick={() => { onChange(b.id); setOpen(false) }}>
                {b.name}
              </li>
            ))}
            {!options?.length && <li className="px-3 py-2 text-gray-500">یافت نشد</li>}
          </ul>
        </div>
      )}
    </div>
  )
}

