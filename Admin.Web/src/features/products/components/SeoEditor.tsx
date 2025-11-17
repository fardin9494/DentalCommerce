import { useEffect, useMemo, useState } from 'react'
import type { ProductDetail } from '../types'
import { useStores } from '../queries'
import { useUpsertProductSeo } from '../queries'

export function SeoEditor({ product }: { product: ProductDetail }) {
  const { data: stores } = useStores()
  const upsert = useUpsertProductSeo(product.id)
  const [storeId, setStoreId] = useState<string>('')
  const [applyAll, setApplyAll] = useState<boolean>(false)
  const [busy, setBusy] = useState<boolean>(false)

  const current = useMemo(() => {
    const sid = storeId || stores?.[0]?.id || ''
    return { sid, seo: product.seos.find(s => s.storeId === sid) }
  }, [storeId, stores, product.seos])

  const [metaTitle, setMetaTitle] = useState('')
  const [metaDescription, setMetaDescription] = useState('')
  const [canonicalUrl, setCanonicalUrl] = useState('')
  const [robots, setRobots] = useState('')
  const [jsonLd, setJsonLd] = useState('')

  const sid = current.sid
  const existing = current.seo

  // Load existing values when store changes (or when existing changes)
  useEffect(() => {
    setMetaTitle(existing?.metaTitle ?? '')
    setMetaDescription(existing?.metaDescription ?? '')
    setCanonicalUrl(existing?.canonicalUrl ?? '')
    setRobots(existing?.robots ?? '')
    setJsonLd(existing?.jsonLd ?? '')
  }, [sid, existing?.metaTitle, existing?.metaDescription, existing?.canonicalUrl, existing?.robots, existing?.jsonLd])

  return (
    <div className="card p-4 space-y-3">
      <h3 className="font-semibold">سئوی فروشگاه ها</h3>
      <div>
        <label className="label">فروشگاه</label>
        <select className="input" value={sid} onChange={e=>setStoreId(e.target.value)}>
          {(stores ?? []).map(s => (
            <option key={s.id} value={s.id}>{s.name} {s.domain ? `(${s.domain})` : ''}</option>
          ))}
        </select>
        <label className="mt-2 inline-flex items-center gap-2 text-sm">
          <input type="checkbox" checked={applyAll} onChange={e=>setApplyAll(e.target.checked)} />
          <span>اعمال برای همه فروشگاه ها</span>
        </label>
      </div>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
        <div>
          <label className="label">Meta Title</label>
          <input className="input" value={metaTitle} onChange={e=>setMetaTitle(e.target.value)} />
        </div>
        <div>
          <label className="label">Meta Description</label>
          <input className="input" value={metaDescription} onChange={e=>setMetaDescription(e.target.value)} />
        </div>
        <div>
          <label className="label">Canonical URL</label>
          <input className="input" value={canonicalUrl} onChange={e=>setCanonicalUrl(e.target.value)} />
        </div>
        <div>
          <label className="label">Robots</label>
          <input className="input" value={robots} onChange={e=>setRobots(e.target.value)} />
        </div>
        <div className="md:col-span-2">
          <label className="label">JSON-LD</label>
          <textarea className="input min-h-28" value={jsonLd} onChange={e=>setJsonLd(e.target.value)} />
        </div>
      </div>
      <div className="pt-2">
        <button className="btn" disabled={(applyAll ? (stores?.length ?? 0) === 0 : !sid) || busy}
                onClick={async ()=>{
          const targets = applyAll ? (stores ?? []).map(s => s.id) : (sid ? [sid] : [])
          if (targets.length === 0) return
          const payload = {
            metaTitle: (metaTitle || '').trim() || null,
            metaDescription: (metaDescription || '').trim() || null,
            canonicalUrl: (canonicalUrl || '').trim() || null,
            robots: (robots || '').trim() || null,
            jsonLd: (jsonLd || '').trim() || null,
          }
          setBusy(true)
          try {
            const results = await Promise.allSettled(targets.map(t => upsert.mutateAsync({ storeId: t, ...payload })))
            const ok = results.filter(r => r.status === 'fulfilled').length
            const fail = results.length - ok
            if (fail === 0) alert(`SEO updated for ${ok} store(s)`) 
            else alert(`SEO saved for ${ok}, failed for ${fail}`)
          } catch (e:any) {
            alert(e?.message || 'Failed to update SEO')
          } finally {
            setBusy(false)
          }
        }}>ذخیره</button>
      </div>
    </div>
  )
}
