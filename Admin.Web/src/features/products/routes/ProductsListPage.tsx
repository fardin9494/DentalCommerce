import { useMemo, useState } from 'react'
import { PageHeader } from '../../../shared/components/PageHeader'
import { Spinner } from '../../../shared/components/Spinner'
import { ProductListTable } from '../components/ProductListTable'
import { useBrands, useCategories, useProducts } from '../queries'

export function ProductsListPage() {
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [brandId, setBrandId] = useState<string>('')
  const [categoryId, setCategoryId] = useState<string>('')
  const [sort, setSort] = useState<string>('')

  const params = useMemo(() => ({
    page,
    pageSize: 10,
    search,
    brandId: brandId || undefined,
    categoryId: categoryId || undefined,
    sort: sort || undefined,
  }), [page, search, brandId, categoryId, sort])

  const { data, isLoading } = useProducts(params)
  const { data: brands } = useBrands()
  const { data: categories } = useCategories()

  // مرتب‌سازی سلسله‌مراتبی (پره‌اُردر): والد ← سپس فرزندان
  const orderedCategories = useMemo(() => {
    const list = categories ?? []
    type Cat = typeof list[number]
    const children = new Map<string | null | undefined, Cat[]>()
    for (const n of list) {
      const key = (n.parentId ?? null) as string | null
      if (!children.has(key)) children.set(key, [])
      children.get(key)!.push(n)
    }
    // مرتب‌سازی هر سطح بر اساس نام
    for (const arr of children.values()) {
      arr.sort((a, b) => a.name.localeCompare(b.name))
    }
    const out: Array<Cat & { hasChildren: boolean }> = []
    const walk = (parentId: string | null | undefined) => {
      const level = children.get((parentId ?? null) as string | null) || []
      for (const n of level) {
        const kids = children.get(n.id) || []
        out.push({ ...n, hasChildren: kids.length > 0 })
        walk(n.id)
      }
    }
    walk(null)
    return out
  }, [categories])

  return (
    <div className="space-y-4">
      <PageHeader title="محصولات" actions={<a href="/products/new" className="btn">ایجاد محصول</a>}>
        مدیریت محصولات و فیلتر براساس جستجو، برند، دسته و مرتب‌سازی
      </PageHeader>

      <div className="card p-4">
        <div className="grid grid-cols-1 md:grid-cols-5 gap-3">
          <input value={search} onChange={e=>setSearch(e.target.value)} className="input" placeholder="جستجو..." />

          <select className="input" value={brandId} onChange={e=>setBrandId(e.target.value)}>
            <option value="">برند</option>
            {brands?.map(b=> <option key={b.id} value={b.id}>{b.name}</option>)}
          </select>

          <select className="input" value={categoryId} onChange={e=>setCategoryId(e.target.value)}>
            <option value="">دسته‌بندی</option>
            {orderedCategories?.map(c => {
              const indent = '\u00A0\u00A0'.repeat(Math.max(0, c.depth - 1))
              const icon = c.hasChildren ? '📁' : '📄'
              return <option key={c.id} value={c.id}>{`${indent}${icon} ${c.name}`}</option>
            })}
          </select>

          <select className="input" value={sort} onChange={e=>setSort(e.target.value)}>
            <option value="">مرتب‌سازی (پیش‌فرض: جدیدترین)</option>
            <option value="name">name</option>
            <option value="-name">-name</option>
            <option value="code">code</option>
            <option value="-code">-code</option>
            <option value="created">created</option>
            <option value="-created">-created</option>
            <option value="updated">updated</option>
            <option value="-updated">-updated</option>
          </select>

          <div className="flex items-center gap-2">
            <button className="btn-secondary px-3 py-2 rounded" onClick={()=>{ setSearch(''); setBrandId(''); setCategoryId(''); setSort(''); setPage(1); }}>پاک‌سازی فیلترها</button>
          </div>
        </div>
      </div>

      <div className="card p-4">
        {isLoading ? <Spinner /> : (
          <>
            <ProductListTable items={data?.items ?? []} />
            <div className="flex items-center justify-between mt-4">
              <button disabled={page<=1} className="btn-secondary px-3 py-2 rounded" onClick={()=>setPage(p=>Math.max(1,p-1))}>قبلی</button>
              <div className="text-sm text-gray-600">صفحه {data?.page ?? page} از {data?.totalPages ?? 1}</div>
              <button disabled={(data?.page ?? 1) >= (data?.totalPages ?? 1)} className="btn-secondary px-3 py-2 rounded" onClick={()=>setPage(p=>p+1)}>بعدی</button>
            </div>
          </>
        )}
      </div>
    </div>
  )
}
