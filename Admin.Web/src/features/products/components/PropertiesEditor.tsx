import { useEffect, useState } from 'react'
import * as api from '../api'

export type PropertyItem = { key: string; value: string }

type Props = {
  value: PropertyItem[]
  onChange: (items: PropertyItem[]) => void
}

export function PropertiesEditor({ value, onChange }: Props) {
  const [draft, setDraft] = useState<PropertyItem>({ key: '', value: '' })
  const [keySuggestions, setKeySuggestions] = useState<Array<{ key: string; usageCount: number }>>([])
  const [valueSuggestions, setValueSuggestions] = useState<string[]>([])

  useEffect(() => {
    api.listPropertyKeys(15).then(setKeySuggestions).catch(() => {})
  }, [])

  useEffect(() => {
    const k = draft.key.trim()
    if (!k) {
      setValueSuggestions([])
      return
    }
    api.listPropertyValues(k, 15).then(setValueSuggestions).catch(() => {})
  }, [draft.key])

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
      {keySuggestions.length > 0 && (
        <div className="flex flex-wrap gap-2 text-xs">
          {keySuggestions.map(k => (
            <button
              key={k.key}
              type="button"
              className="badge badge-gray"
              onClick={() => setDraft(d => ({ ...d, key: k.key }))}
            >
              {k.key} ({k.usageCount})
            </button>
          ))}
        </div>
      )}
      {valueSuggestions.length > 0 && (
        <div className="flex flex-wrap gap-2 text-xs">
          {valueSuggestions.map(v => (
            <button
              key={v}
              type="button"
              className="badge badge-green"
              onClick={() => setDraft(d => ({ ...d, value: v }))}
            >
              {v}
            </button>
          ))}
        </div>
      )}
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
