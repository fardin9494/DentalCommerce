import React, { useState } from 'react'
import { useParams } from 'react-router-dom'
import { PageHeader } from '../../../shared/components/PageHeader'
import { Spinner } from '../../../shared/components/Spinner'
import { ProductImages } from '../components/ProductImages'
import { PropertiesEditor } from '../components/PropertiesEditor'
import { DescriptionEditor } from '../components/DescriptionEditor'
import { SeoEditor } from '../components/SeoEditor'
import { ProductStoresEditor } from '../components/ProductStoresEditor'
import { useAddVariant, useDeleteVariant, useSetCategories, useUpdateBasics } from '../queries'
import { BrandSelect } from '../components/BrandSelect'
import { CategoryMultiSelect } from '../components/CategoryMultiSelect'
import { useBrands, useCategories } from '../queries'
import { useProduct } from '../queries'
import { useQueryClient } from '@tanstack/react-query'
import * as api from '../api'

export function ProductDetailPage() {
  const { id } = useParams<{ id: string }>()
  const { data: product, isLoading } = useProduct(id)
  const qc = useQueryClient()
  const pid = id ?? ''
  const addVariant = useAddVariant(pid)
  const deleteVariant = useDeleteVariant(pid)
  const updateBasics = useUpdateBasics(pid)
  const setCats = useSetCategories(pid)
  const { data: brands } = useBrands()
  const { data: cats } = useCategories()

  const [name, setName] = useState('')
  const [slug, setSlug] = useState('')
  const [code, setCode] = useState('')
  const [warehouseCode, setWarehouseCode] = useState('')
  const [brandId, setBrandId] = useState<string>('')
  const [categoryIds, setCategoryIds] = useState<string[]>([])
  const [primaryCatId, setPrimaryCatId] = useState<string>('')
  const [desc, setDesc] = useState('')

  React.useEffect(() => {
    if (product) {
      setDesc(product.description ?? '')
      setPrimaryCatId(product.primaryCategoryId || '')
    }
  }, [product?.id])

  React.useEffect(() => {
    const selected = (categoryIds.length ? categoryIds : product?.categories.map(c=>c.categoryId) || [])
    if (!selected.includes(primaryCatId || '')) {
      setPrimaryCatId(selected[0] || '')
    }
  }, [categoryIds, product?.id])

  if (isLoading) return <Spinner />
  if (!product) return <div className="text-sm">Ù…Ø­ØµÙˆÙ„ ÛŒØ§ÙØª Ù†Ø´Ø¯.</div>

  return (
    <div className="space-y-4">
      <PageHeader title={`Ø¬Ø²Ø¦ÛŒØ§Øª Ù…Ø­ØµÙˆÙ„: ${product.name}`} />
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        <div className="lg:col-span-2 card p-4 space-y-2">
          <h3 className="font-semibold">Ù…Ø´Ø®ØµØ§Øª (Ù‚Ø§Ø¨Ù„ ÙˆÛŒØ±Ø§ÛŒØ´)</h3>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <div>
              <label className="label">Ù†Ø§Ù…</label>
              <input className="input" value={name || product.name} onChange={e=>setName(e.target.value)} />
            </div>
            <div>
              <label className="label">Slug</label>
              <input className="input" value={slug || product.defaultSlug} onChange={e=>setSlug(e.target.value)} />
            </div>
            <div>
              <label className="label">Ú©Ø¯</label>
              <input className="input" value={code || product.code} onChange={e=>setCode(e.target.value)} />
            </div>
            <div>
              <label className="label">Ú©Ø¯ Ø§Ù†Ø¨Ø§Ø±</label>
              <input className="input" value={warehouseCode || product.warehouseCode || ''} onChange={e=>setWarehouseCode(e.target.value)} />
            </div>
            <div className="md:col-span-2">
              <BrandSelect value={brandId || product.brandId} onChange={(id)=> setBrandId(id)} />
            </div>
          </div>
          <div className="pt-2">
            <button className="btn" onClick={async ()=>{
              try {
                await updateBasics.mutateAsync({
                  name: name || product.name,
                  slug: slug || product.defaultSlug,
                  code: code || product.code,
                  warehouseCode: (warehouseCode !== '' ? warehouseCode : (product.warehouseCode ?? null)),
                  brandId: (brandId || product.brandId)!,
                })
                alert('Ø°Ø®ÛŒØ±Ù‡ Ø´Ø¯')
              } catch (e:any) { alert(e?.message || 'Ø®Ø·Ø§ Ø¯Ø± Ø°Ø®ÛŒØ±Ù‡ Ù…Ø´Ø®ØµØ§Øª') }
            }}>Ø°Ø®ÛŒØ±Ù‡ Ù…Ø´Ø®ØµØ§Øª</button>
          </div>
          <div className="pt-3 flex gap-2">
            {product.status === 'Active' ? (
              <button className="btn-red" onClick={async () => {
                try { await api.hideProduct(product.id); await qc.invalidateQueries({ queryKey: ['products','detail', product.id] }); alert('Ù…Ø®ÙÛŒ Ø´Ø¯') } catch (e:any) { alert(e?.message || 'Ø®Ø·Ø§ Ø¯Ø± Ù…Ø®ÙÛŒâ€ŒØ³Ø§Ø²ÛŒ') }
              }}>Ù…Ø®ÙÛŒ Ú©Ø±Ø¯Ù†</button>
            ) : (
              <button className="btn-green" onClick={async () => {
                try { await api.activateProduct(product.id); await qc.invalidateQueries({ queryKey: ['products','detail', product.id] }); alert('ÙØ¹Ø§Ù„ Ø´Ø¯') } catch (e:any) { alert(e?.message || 'Ø®Ø·Ø§ Ø¯Ø± ÙØ¹Ø§Ù„â€ŒØ³Ø§Ø²ÛŒ') }
              }}>ÙØ¹Ø§Ù„â€ŒØ³Ø§Ø²ÛŒ</button>
            )}
          </div>
        </div>
        <ProductImages product={product} />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        <div className="lg:col-span-2 card p-4 space-y-3">
          <h3 className="font-semibold">ÙˆÛŒØ±Ø§ÛŒØ´ ØªÙˆØ¶ÛŒØ­Ø§Øª</h3>
          <DescriptionEditor value={desc} onChange={setDesc} />
          <button className="btn" onClick={async () => {
            try { await api.setProductDescription(product.id, desc); await qc.invalidateQueries({ queryKey: ['products','detail', product.id] }); alert('Ø°Ø®ÛŒØ±Ù‡ Ø´Ø¯'); } catch (e:any) { alert(e?.message || 'Ø®Ø·Ø§ Ø¯Ø± Ø°Ø®ÛŒØ±Ù‡ ØªÙˆØ¶ÛŒØ­Ø§Øª') }
          }}>Ø°Ø®ÛŒØ±Ù‡ ØªÙˆØ¶ÛŒØ­Ø§Øª</button>
          <div className="pt-4 space-y-2">
            <h3 className="font-semibold">ØªÙ†ÙˆØ¹ Ù…Ø­ØµÙˆÙ„</h3>
            <div className="text-xs text-gray-600">Ú©Ù„ÛŒØ¯ ØªÙ†ÙˆØ¹ ÙØ¹Ù„ÛŒ: {product.variationKey ?? 'â€”'}</div>
            <div className="flex items-center gap-2">
              <input id="vk" className="input" placeholder="Variation Key (Ù…Ø«Ù„Ø§Ù‹ Ø±Ù†Ú¯)" />
              <button className="btn" onClick={async ()=>{
                const vk = (document.getElementById('vk') as HTMLInputElement)?.value || ''
                try { await api.setVariation(product.id, vk || null); await qc.invalidateQueries({ queryKey: ['products','detail', product.id] }); alert('Ø°Ø®ÛŒØ±Ù‡ Ø´Ø¯') } catch(e:any){ alert(e?.message || 'Ø®Ø·Ø§ Ø¯Ø± Ø°Ø®ÛŒØ±Ù‡ Variation Key') }
              }}>Ø°Ø®ÛŒØ±Ù‡</button>
            </div>

            <div className="border rounded">
              <div className="grid grid-cols-5 gap-2 px-3 py-2 bg-gray-50 text-xs">
                <div className="col-span-2">Ù…Ù‚Ø¯Ø§Ø±</div>
                <div className="col-span-2">SKU</div>
                <div>ÙˆØ¶Ø¹ÛŒØª</div>
              </div>
              {product.variants.map(v => (
                <div key={v.id} className="grid grid-cols-5 gap-2 px-3 py-2 border-b last:border-0 text-sm items-center">
                  <div className="col-span-2">{v.value}</div>
                  <div className="col-span-2 font-mono">{v.sku}</div>
                  <div className="flex items-center gap-2">
                    <span>{v.isActive ? 'ÙØ¹Ø§Ù„' : 'ØºÛŒØ±ÙØ¹Ø§Ù„'}</span>
                    <button className="btn-red px-2 py-1" onClick={async ()=>{ if(!confirm('Ø­Ø°Ù Ø´ÙˆØ¯ØŸ')) return; try { await deleteVariant.mutateAsync(v.id) } catch(e:any){ alert(e?.message || 'Ø®Ø·Ø§ Ø¯Ø± Ø­Ø°Ù') } }}>Ø­Ø°Ù</button>
                  </div>
                </div>
              ))}
              <div className="grid grid-cols-5 gap-2 px-3 py-2">
                <input id="nv" className="input sm:col-span-2" placeholder="Ù…Ù‚Ø¯Ø§Ø± (Ù…Ø«Ù„Ø§Ù‹ Ù‚Ø±Ù…Ø²)" />
                <input id="ns" className="input sm:col-span-2" placeholder="SKU" />
                <label className="flex items-center gap-2 text-sm"><input id="na" type="checkbox" defaultChecked /> ÙØ¹Ø§Ù„</label>
                <div className="col-span-5">
                  <button className="btn" onClick={async ()=>{
                    const value = (document.getElementById('nv') as HTMLInputElement)?.value || ''
                    const sku = (document.getElementById('ns') as HTMLInputElement)?.value || ''
                    const isActive = (document.getElementById('na') as HTMLInputElement)?.checked ?? true
                    try { await addVariant.mutateAsync({ value, sku, isActive }) } catch(e:any){ alert(e?.message || 'Ø®Ø·Ø§ Ø¯Ø± Ø§ÙØ²ÙˆØ¯Ù† ØªÙ†ÙˆØ¹') }
                  }}>Ø§ÙØ²ÙˆØ¯Ù† ØªÙ†ÙˆØ¹</button>
                </div>
              </div>
            </div>
          </div>
        </div>
        <div className="card p-4 space-y-2">
          <h3 className="font-semibold">ÙˆÛŒÚ˜Ú¯ÛŒâ€ŒÙ‡Ø§</h3>
          <div className="border rounded">
            <div className="grid grid-cols-6 gap-2 px-3 py-2 bg-gray-50 text-xs">
              <div className="col-span-2">Ú©Ù„ÛŒØ¯</div>
              <div className="col-span-3">Ù…Ù‚Ø¯Ø§Ø±</div>
              <div>Ø¹Ù…Ù„ÛŒØ§Øª</div>
            </div>
            {product.properties.map(p => (
              <div key={p.id} className="grid grid-cols-6 gap-2 px-3 py-2 border-b last:border-0 items-center text-sm">
                <div className="col-span-2 font-mono">{p.key}</div>
                <div className="col-span-3 truncate">{p.valueString ?? ''}</div>
                <div className="flex items-center gap-2">
                  <button className="btn-red px-2 py-1" onClick={async ()=>{
                    if (!confirm('Ø­Ø°Ù Ø´ÙˆØ¯ØŸ')) return
                    try { await api.deleteProperty(product.id, p.id); await qc.invalidateQueries({ queryKey: ['products','detail', product.id] }); alert('Ø­Ø°Ù Ø´Ø¯') } catch(e:any){ alert(e?.message || 'Ø®Ø·Ø§ Ø¯Ø± Ø­Ø°Ù') }
                  }}>Ø­Ø°Ù</button>
                </div>
              </div>
            ))}
          </div>
          <div className="pt-2">
            <h4 className="font-semibold">Ø§ÙØ²ÙˆØ¯Ù† ÙˆÛŒÚ˜Ú¯ÛŒ Ø¬Ø¯ÛŒØ¯</h4>
            <PropertiesEditor value={[]} onChange={async (items)=>{
              try {
                for (const it of items) {
                  if (it.key.trim()) await api.upsertProductProperty(product.id, { key: it.key.trim(), valueString: it.value })
                }
                await qc.invalidateQueries({ queryKey: ['products','detail', product.id] });
                alert('Ø°Ø®ÛŒØ±Ù‡ Ø´Ø¯')
              } catch(e:any){ alert(e?.message || 'Ø®Ø·Ø§ Ø¯Ø± Ø§ÙØ²ÙˆØ¯Ù† ÙˆÛŒÚ˜Ú¯ÛŒ') }
            }} />
          </div>

          <div className="pt-4 space-y-2">
            <h3 className="font-semibold">Ø¯Ø³ØªÙ‡â€ŒØ¨Ù†Ø¯ÛŒ</h3>
            <CategoryMultiSelect value={categoryIds.length ? categoryIds : product.categories.map(c=>c.categoryId)} onChange={setCategoryIds} />
            <button className="btn" onClick={async ()=>{
              try { await setCats.mutateAsync({ categoryIds: (categoryIds.length ? categoryIds : product.categories.map(c=>c.categoryId)) }) ; alert("Saved") } catch(e:any){ alert(e?.message || "Failed to update categories") }
            }}>Ø°Ø®ÛŒØ±Ù‡ Ø¯Ø³ØªÙ‡â€ŒÙ‡Ø§</button>
          </div>
        </div>
          <div className="card p-4 space-y-2">
            <h3 className="font-semibold">Primary category</h3>
            <div className="border rounded p-2 max-h-48 overflow-auto text-sm space-y-1">
              {(categoryIds.length ? categoryIds : product.categories.map(c=>c.categoryId)).map((cid) => {
                const name = (cats || []).find(c=>c.id===cid)?.name || cid
                return (
                  <label key={cid} className="flex items-center gap-2">
                    <input type="radio" name="primaryCat2" value={cid} checked={(primaryCatId || '')===cid} onChange={()=>setPrimaryCatId(cid)} />
                    <span>{name}</span>
                  </label>
                )
              })}
              {!(categoryIds.length ? categoryIds : product.categories.map(c=>c.categoryId)).length && (
                <div className="text-xs text-gray-500">No categories selected</div>
              )}
            </div>
            <button className="btn" onClick={async ()=>{
              const ids = (categoryIds.length ? categoryIds : product.categories.map(c=>c.categoryId))
              try { await setCats.mutateAsync({ categoryIds: ids, primaryCategoryId: primaryCatId || undefined }) ; alert("Saved") } catch(e:any){ alert(e?.message || 'Failed to set primary category') }
            }}>Save primary</button>
          </div>
      </div>

      <SeoEditor product={product} />
      <ProductStoresEditor product={product} />
    </div>
  )
}
