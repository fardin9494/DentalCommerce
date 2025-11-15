import { useState } from 'react'

export type PropertyItem = { key: string; value: string }

type Props = {
  value: PropertyItem[]
  onChange: (items: PropertyItem[]) => void
}

export function PropertiesEditor({ value, onChange }: Props) {
  const [draft, setDraft] = useState<PropertyItem>({ key: '', value: '' })

  const add = () => {
    if (!draft.key.trim()) return
    onChange([...value, { key: draft.key.trim(), value: draft.value }])
    setDraft({ key: '', value: '' })
  }
  const remove = (i: number) => onChange(value.filter((_, idx) => idx !== i))

  return (
    <div className="space-y-2">
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-2">
        <input className="input" placeholder="کلید" value={draft.key} onChange={e=>setDraft(d=>({ ...d, key: e.target.value }))} />
        <input className="input sm:col-span-2" placeholder="مقدار" value={draft.value} onChange={e=>setDraft(d=>({ ...d, value: e.target.value }))} />
      </div>
      <button type="button" className="btn" onClick={add}>افزودن</button>
      {value.length > 0 && (
        <div className="border rounded">
          {value.map((it, i) => (
            <div key={i} className="flex items-center justify-between gap-2 px-3 py-2 border-b last:border-0 text-sm">
              <div className="font-mono">{it.key}</div>
              <div className="flex-1 truncate">{it.value}</div>
              <button type="button" className="btn-secondary px-2 py-1 rounded" onClick={() => remove(i)}>حذف</button>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

