import { useMemo } from 'react'

type PermissionItem = {
  key: string
  title: string
  description: string
  granted: boolean
}

type Props = {
  items: PermissionItem[]
  disabled?: boolean
  onChange: (key: string, granted: boolean) => void
}

export function PermissionToggleList({ items, disabled, onChange }: Props) {
  const groups = useMemo(() => {
    const map = new Map<string, PermissionItem[]>()
    for (const item of items) {
      const section = item.key.split('.')[1] ?? 'General'
      if (!map.has(section)) map.set(section, [])
      map.get(section)!.push(item)
    }
    return Array.from(map.entries())
  }, [items])

  return (
    <div className="space-y-4">
      {groups.map(([section, list]) => (
        <div key={section} className="border rounded p-3">
          <div className="text-sm font-semibold mb-2">{section}</div>
          <div className="grid md:grid-cols-2 gap-2 text-sm">
            {list.map(p => (
              <label key={p.key} className={`flex items-start gap-2 border rounded p-2 ${disabled ? 'opacity-60' : ''}`}>
                <input
                  type="checkbox"
                  checked={p.granted}
                  disabled={disabled}
                  onChange={e => onChange(p.key, e.target.checked)}
                />
                <div>
                  <div className="font-medium">{p.title}</div>
                  <div className="text-xs text-gray-500">{p.description}</div>
                  <div className="text-[10px] text-gray-400 mt-1">{p.key}</div>
                </div>
              </label>
            ))}
          </div>
        </div>
      ))}
    </div>
  )
}
