import { useRef, useState } from 'react'
import type { ProductDetail } from '../types'
import { useDeleteImage, useReorderImages, useSetMainImage, useUploadProductImage } from '../queries'
import { toPublicMediaUrl } from '../../../lib/api/client'

type Props = { product: ProductDetail }

export function ProductImages({ product }: Props) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [busy, setBusy] = useState(false)
  const [alt, setAlt] = useState('')
  const upload = useUploadProductImage(product.id)
  const setMain = useSetMainImage(product.id)
  const reorder = useReorderImages(product.id)
  const del = useDeleteImage(product.id)
  const [order, setOrder] = useState(product.images.map(i => i.id))

  const onFile = async (file?: File) => {
    if (!file) return
    setBusy(true)
    try { await upload.mutateAsync({ file, alt: alt || undefined }) }
    catch (e: any) { alert(e?.message || 'خطا در آپلود') }
    finally { setBusy(false) }
  }

  // drag and drop
  const dragId = useRef<string | null>(null)
  const handleDragStart = (id: string) => (e: React.DragEvent) => { dragId.current = id; e.dataTransfer.effectAllowed = 'move' }
  const handleDragOver = (id: string) => (e: React.DragEvent) => { e.preventDefault(); e.dataTransfer.dropEffect = 'move' }
  const handleDrop = (id: string) => async (e: React.DragEvent) => {
    e.preventDefault()
    const from = dragId.current
    dragId.current = null
    if (!from || from === id) return
    const current = [...order]
    const fromIdx = current.indexOf(from)
    const toIdx = current.indexOf(id)
    if (fromIdx === -1 || toIdx === -1) return
    current.splice(toIdx, 0, current.splice(fromIdx, 1)[0])
    setOrder(current)
    try { await reorder.mutateAsync(current) } catch (err:any) { alert(err?.message || 'خطا در تغییر ترتیب') }
  }

  return (
    <div className="card p-4">
      <div className="flex items-center justify-between mb-3">
        <h3 className="font-semibold">تصاویر</h3>
        <div className="flex items-center gap-2">
          <input className="input" placeholder="ALT (اجباری)" value={alt} onChange={e=>setAlt(e.target.value)} />
          <input ref={inputRef} type="file" className="hidden" accept="image/*" onChange={(e) => onFile(e.target.files?.[0])} />
          <button className="btn" onClick={() => inputRef.current?.click()} disabled={busy}>آپلود عکس</button>
        </div>
      </div>
      {product.images?.length ? (
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
          {product.images.map(img => (
            <div key={img.id}
                 draggable
                 onDragStart={handleDragStart(img.id)}
                 onDragOver={handleDragOver(img.id)}
                 onDrop={handleDrop(img.id)}
                 className={`relative group border rounded overflow-hidden ${product.images.find(x=>x.id===img.id)?.isMain ? 'ring-2 ring-gray-900' : ''}`}>
              <img src={toPublicMediaUrl(img.url)} alt="product" className="w-full h-32 object-cover" />
              <div className="absolute inset-x-0 bottom-0 bg-black/50 text-white text-xs opacity-0 group-hover:opacity-100 transition flex items-center justify-between px-2 py-1">
                <button className="underline" onClick={async ()=>{ try { await setMain.mutateAsync(img.id) } catch(e:any){ alert(e?.message || 'خطا در تنظیم به عنوان اصلی') } }}>اصلی</button>
                <button className="underline" onClick={async ()=>{ if(!confirm('حذف شود؟')) return; try { await del.mutateAsync(img.id) } catch(e:any){ alert(e?.message || 'خطا در حذف') } }}>حذف</button>
              </div>
            </div>
          ))}
        </div>
      ) : (
        <p className="text-sm text-gray-500">عکسی ثبت نشده است.</p>
      )}
    </div>
  )
}
