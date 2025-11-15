import { useMemo, useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import type { ProductDetail } from '../types'
import { useStores } from '../queries'
import { useUpsertProductStore, useDeleteProductStore } from '../queries'

export function ProductStoresEditor({ product }: { product: ProductDetail }) {
  const { data: stores } = useStores()
  const upsert = useUpsertProductStore(product.id)
  const remove = useDeleteProductStore(product.id)
  const qc = useQueryClient()
  const current = useMemo(() => new Map(product.stores.map(s => [s.storeId, s])), [product.stores])
  type Row = { inStore: boolean; isVisible: boolean; slug: string; titleOverride: string; descriptionOverride: string }
  const [form, setForm] = useState<Record<string, Row>>(() => {
    const m: Record<string, Row> = {}
    for (const st of product.stores) {
      m[st.storeId] = {
        inStore: true,
        isVisible: st.isVisible,
        slug: st.slug || '',
        titleOverride: st.titleOverride ?? '',
        descriptionOverride: st.descriptionOverride ?? '',
      }
    }
    return m
  })
  const [busy, setBusy] = useState(false)

  const allChecked = (stores ?? []).length > 0 && (stores ?? []).every(s => (form[s.id]?.inStore ?? false))
  const toggleAll = (v: boolean) => {
    const next: Record<string, Row> = { ...form }
    for (const st of (stores ?? [])) {
      const prev = next[st.id] || { inStore: false, isVisible: false, slug: '', titleOverride: '', descriptionOverride: '' }
      next[st.id] = { ...prev, inStore: v, isVisible: v ? (prev.isVisible) : false }
    }
    setForm(next)
  }

  return (
    <div className="card p-4 space-y-3">
      <h3 className="font-semibold">Store availability</h3>
      <div className="border rounded overflow-x-auto">
        <div className="grid grid-cols-12 gap-2 px-3 py-2 bg-gray-50 text-xs">
          <div className="col-span-2 flex items-center gap-2">
            <input id="chk-all" type="checkbox" checked={allChecked} onChange={e=>toggleAll(e.target.checked)} />
            <label htmlFor="chk-all" className="font-medium">In store</label>
          </div>
          <div className="col-span-2">Visible</div>
          <div className="col-span-3">Store</div>
          <div className="col-span-3">Slug</div>
          <div className="col-span-2">Title override</div>
          <div className="col-span-2">Description override</div>
        </div>
        {(stores ?? []).map(st => {
          const row = form[st.id] || { inStore: false, isVisible: false, slug: '', titleOverride: '', descriptionOverride: '' }
          const placeholderSlug = product.defaultSlug
          return (
            <div key={st.id} className="grid grid-cols-12 gap-2 px-3 py-2 border-t items-center text-sm">
              <div className="col-span-2">
                <input type="checkbox" checked={row.inStore} onChange={e=>setForm(prev=>({ ...prev, [st.id]: { ...row, inStore: e.target.checked, isVisible: e.target.checked ? row.isVisible : false } }))} />
              </div>
              <div className="col-span-2">
                <input type="checkbox" disabled={!row.inStore} checked={row.isVisible} onChange={e=>setForm(prev=>({ ...prev, [st.id]: { ...row, isVisible: e.target.checked } }))} />
              </div>
              <div className="col-span-3 truncate">
                {st.name} {st.domain ? `(${st.domain})` : ''}
              </div>
              <div className="col-span-3">
                <input className="input" disabled={!row.inStore} placeholder={placeholderSlug} value={row.slug}
                       onChange={e=>setForm(prev=>({ ...prev, [st.id]: { ...row, slug: e.target.value } }))} />
              </div>
              <div className="col-span-2">
                <input className="input" disabled={!row.inStore} placeholder="Optional" value={row.titleOverride}
                       onChange={e=>setForm(prev=>({ ...prev, [st.id]: { ...row, titleOverride: e.target.value } }))} />
              </div>
              <div className="col-span-2">
                <input className="input" disabled={!row.inStore} placeholder="Optional" value={row.descriptionOverride}
                       onChange={e=>setForm(prev=>({ ...prev, [st.id]: { ...row, descriptionOverride: e.target.value } }))} />
              </div>
            </div>
          )
        })}
      </div>
      <div>
        <button className="btn" disabled={busy || (stores ?? []).length===0} onClick={async ()=>{
          setBusy(true)
          try {
            // Build only real operations (delete or upsert), skip no-ops
            const ops = (stores ?? []).flatMap(st => {
              const row = form[st.id] || { inStore: false, isVisible: false, slug: '', titleOverride: '', descriptionOverride: '' }
              const existing = current.get(st.id)
              const isVisible = !!row.isVisible
              const inStore = !!row.inStore
              const slugToSend = (row.slug?.trim() || existing?.slug || product.defaultSlug)
              const titleOverride = (row.titleOverride?.trim() || null)
              const descriptionOverride = (row.descriptionOverride?.trim() || null)

              if (!inStore && existing) {
                return [{ kind: 'delete' as const, name: st.name, p: remove.mutateAsync(st.id) }]
              }
              if (inStore) {
                const same = existing && existing.isVisible === isVisible
                  && existing.slug === slugToSend
                  && (existing.titleOverride ?? null) === titleOverride
                  && (existing.descriptionOverride ?? null) === descriptionOverride
                if (!same) {
                  return [{ kind: 'upsert' as const, name: st.name, p: upsert.mutateAsync({ storeId: st.id, isVisible, slug: slugToSend, titleOverride, descriptionOverride }) }]
                }
              }
              return [] as const
            })

            const results = await Promise.allSettled(ops.map(x => x.p))
            const ok = results.filter(r => r.status === 'fulfilled').length
            const total = ops.length
            if (ok === total) alert(`Saved store settings for ${ok}/${total} change(s)`) 
            else {
              // surface failures
              const fails: string[] = []
              results.forEach((r, i) => { if (r.status === 'rejected') fails.push(ops[i]?.name || `#${i+1}`) })
              alert(`Saved ${ok}/${total}. Failed for: ${fails.join(', ')}`)
            }
            await qc.invalidateQueries({ queryKey: ['products','detail', product.id] })
          } catch (e:any) {
            alert(e?.message || 'Failed to update stores')
          } finally {
            setBusy(false)
          }
        }}>Save availability</button>
      </div>
    </div>
  )
}
