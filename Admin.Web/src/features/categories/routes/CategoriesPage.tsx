
import { useMemo, useRef, useState } from 'react'
import { PageHeader } from '../../../shared/components/PageHeader'
import { Spinner } from '../../../shared/components/Spinner'
import {
  useCategories,
  useCreateCategory,
  useMoveCategoryNoConfirm,
  useRenameCategory,
  useLeafCategoriesWithProducts,
} from '../../products/queries'
import { swalConfirm, swalPrompt, swalToastError, swalToastSuccess } from '../../../shared/utils/swal'

type FlatNode = { id: string; name: string; slug: string; parentId?: string | null; depth: number }
type TreeNode = FlatNode & { children: TreeNode[] }

export function CategoriesPage() {
  const { data: nodes, isLoading } = useCategories()
  const create = useCreateCategory()
  const rename = useRenameCategory()
  const move = useMoveCategoryNoConfirm()
  const { data: leavesWithProducts } = useLeafCategoriesWithProducts()
  const blockedTargets = useMemo(() => new Set((leavesWithProducts ?? []).map((x) => x.id)), [leavesWithProducts])

  const [form, setForm] = useState({ name: '', slug: '', parentId: '' })
  const dragId = useRef<string | null>(null)
  const [expanded, setExpanded] = useState<Record<string, boolean>>({})
  const isExpanded = (id: string) => expanded[id] ?? true

  const idToName = useMemo(() => {
    const m = new Map<string, string>()
    ;(nodes ?? []).forEach((n) => m.set(n.id, n.name))
    return m
  }, [nodes])

  const tree = useMemo<TreeNode[]>(() => {
    const all = (nodes ?? []) as FlatNode[]
    const map = new Map<string, TreeNode>()
    all.forEach((n) => map.set(n.id, { ...n, children: [] }))
    const roots: TreeNode[] = []
    map.forEach((n) => {
      if (n.parentId && map.has(n.parentId)) map.get(n.parentId)!.children.push(n)
      else roots.push(n)
    })
    const sortRec = (arr: TreeNode[]) => {
      arr.sort((a, b) => a.name.localeCompare(b.name))
      arr.forEach((ch) => sortRec(ch.children))
    }
    sortRec(roots)
    return roots
  }, [nodes])

  const possibleParents = useMemo<TreeNode[]>(() => {
    const acc: TreeNode[] = []
    const walk = (arr: TreeNode[]) => {
      arr.forEach((n) => {
        acc.push(n)
        if (n.children.length) walk(n.children)
      })
    }
    walk(tree)
    return acc
  }, [tree])

  return (
    <div className="space-y-4">
      <PageHeader title="مدیریت دسته‌ها">مدیریت دسته‌ها</PageHeader>
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        <div className="lg:col-span-2 card p-4">
          {isLoading ? (
            <Spinner />
          ) : (
            <div>
              <div className="text-xs text-gray-600 mb-2">
                نکته: افزودن زیر‌دسته به دسته‌ای که برای آن محصول ثبت شده باشد مجاز نیست.
              </div>

              {/* انتقال به ریشه */}
              <div
                className="mb-2 p-2 border rounded bg-gray-50 text-xs text-gray-700"
                onDragOver={(e) => e.preventDefault()}
                onDrop={async (e) => {
                  e.preventDefault()
                  const from = dragId.current
                  dragId.current = null
                  if (!from) return
                  const fromName = idToName.get(from) || from
                  const ok = await swalConfirm({
                    title: 'انتقال به ریشه',
                    text: `آیا از انتقال «${fromName}» به ریشه مطمئن هستید؟`,
                    icon: 'warning',
                    confirmText: 'انتقال',
                    cancelText: 'انصراف',
                  })
                  if (!ok) return
                  try {
                    await move.mutateAsync({ id: from, newParentId: null })
                    swalToastSuccess('انتقال انجام شد')
                  } catch (e: any) {
                    const msg = (e?.details?.detail || e?.message || '').toString()
                    swalToastError(msg || 'خطا در انتقال به ریشه')
                  }
                }}
              >
                برای انتقال دسته به ریشه، آن را اینجا رها کنید.
              </div>

              <CategoryTree
                nodes={tree}
                isExpanded={isExpanded}
                onToggle={(id) => setExpanded((s) => ({ ...s, [id]: !isExpanded(id) }))}
                onRename={async (id, prev) => {
                  const name = await swalPrompt({
                    title: 'ویرایش دسته',
                    defaultValue: prev.name,
                    required: true,
                    confirmText: 'ذخیره',
                    cancelText: 'انصراف',
                  })
                  if (!name) return
                  try {
                    await rename.mutateAsync({ id, name, slug: prev.slug })
                    swalToastSuccess('دسته بروزرسانی شد')
                  } catch (err: any) {
                    swalToastError(err?.message || 'خطا در بروزرسانی دسته')
                  }
                }}
                onDragStart={(id) => {
                  dragId.current = id
                }}
                onDragOver={(e) => e.preventDefault()}
                onDrop={async (targetId: string) => {
                  // جلوگیری از دراپ روی برگ‌هایی که محصول دارند
                  if (blockedTargets.has(targetId)) {
                    swalToastError('نمی‌توانید درون دسته‌ای که محصول دارد، زیر‌دسته اضافه کنید')
                    return
                  }

                  const from = dragId.current
                  dragId.current = null
                  if (!from || from === targetId) return
                  const fromName = idToName.get(from) || from
                  const targetName = idToName.get(targetId) || targetId
                  const ok = await swalConfirm({
                    title: 'انتقال دسته',
                    text: `آیا از انتقال «${fromName}» به «${targetName}» مطمئن هستید؟`,
                    icon: 'warning',
                    confirmText: 'انتقال',
                    cancelText: 'انصراف',
                  })
                  if (!ok) return
                  try {
                    await move.mutateAsync({ id: from, newParentId: targetId })
                    swalToastSuccess('انتقال انجام شد')
                  } catch (e: any) {
                    const status = e?.status as number | undefined
                    const msg = (e?.details?.detail || e?.message || '').toString()
                    if (status === 400) swalToastError('امکان افزودن زیر‌دسته به دسته‌ای که محصول دارد وجود ندارد')
                    else swalToastError(msg || 'خطا در انتقال دسته')
                  }
                }}
              />
            </div>
          )}
        </div>

        {/* فرم ایجاد دسته */}
        <div className="card p-4">
          <h3 className="font-semibold mb-3">ایجاد دسته</h3>
          <form
            className="space-y-3"
            onSubmit={async (e) => {
              e.preventDefault()
              try {
                await create.mutateAsync({ name: form.name.trim(), slug: form.slug.trim(), parentId: form.parentId || null })
                setForm({ name: '', slug: '', parentId: '' })
                swalToastSuccess('دسته ایجاد شد')
              } catch (err: any) {
                swalToastError(err?.message || 'خطا در ایجاد دسته')
              }
            }}
          >
            <div>
              <label className="label">نام</label>
              <input className="input" value={form.name} onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))} />
            </div>
            <div>
              <label className="label">مسیر (Slug)</label>
              <input className="input" value={form.slug} onChange={(e) => setForm((f) => ({ ...f, slug: e.target.value }))} />
            </div>
            <div>
              <label className="label">والد</label>
              <div className="border rounded p-2 max-h-64 overflow-auto space-y-1 text-sm">
                <label className="flex items-center gap-2">
                  <input type="radio" name="parentCat" value="" checked={!form.parentId} onChange={() => setForm((f) => ({ ...f, parentId: '' }))} />
                  <span>ریشه (بدون والد)</span>
                </label>
                {possibleParents.map((n) => (
                  <label key={n.id} className="flex items-center gap-2">
                    <input type="radio" name="parentCat" value={n.id} checked={form.parentId === n.id} onChange={() => setForm((f) => ({ ...f, parentId: n.id }))} />
                    <span className="text-gray-400">{'›'.repeat(Math.max(0, (n.depth ?? 1) - 1))}</span>
                    <span>{n.name}</span>
                  </label>
                ))}
              </div>
            </div>
            <button type="submit" className="btn">ایجاد</button>
          </form>
        </div>
      </div>
    </div>
  )
}

function CategoryTree({ nodes, isExpanded, onToggle, onRename, onDragStart, onDragOver, onDrop }: {
  nodes: TreeNode[]
  isExpanded: (id: string) => boolean
  onToggle: (id: string) => void
  onRename: (id: string, node: { name: string; slug: string }) => void
  onDragStart: (id: string) => void
  onDragOver: (e: React.DragEvent) => void
  onDrop: (targetId: string) => void
}) {
  return (
    <ul className="text-sm select-none">
      {nodes.map((n) => (
        <li key={n.id} className="py-1">
          <div
            className="flex items-center justify-between gap-2 border rounded px-2 py-1 hover:bg-gray-50"
            draggable
            onDragStart={() => onDragStart(n.id)}
            onDragOver={onDragOver}
            onDrop={() => onDrop(n.id)}
          >
            <div className="flex items-center gap-2 w-full">
              {n.children.length > 0 ? (
                <button type="button" className="btn-secondary px-2 py-0.5 rounded" onClick={() => onToggle(n.id)}>
                  {isExpanded(n.id) ? '−' : '+'}
                </button>
              ) : (
                <span className="px-2" />
              )}
              {/* ایندنت از راست */}
              <span className="flex-1 text-right pr-2" style={{ paddingRight: `${Math.max(0, (n.depth ?? 1) - 1) * 12}px` }}>
                {n.name}
                <span className="text-xs text-gray-500"> ({n.slug})</span>
              </span>
              <div className="flex items-center gap-2">
                <button className="btn-secondary px-2 py-1 rounded" onClick={() => onRename(n.id, { name: n.name, slug: n.slug })}>
                  ویرایش
                </button>
              </div>
            </div>
          </div>
          {n.children.length > 0 && isExpanded(n.id) && (
            <div className="pr-6 mt-1">
              <CategoryTree
                nodes={n.children}
                isExpanded={isExpanded}
                onToggle={onToggle}
                onRename={onRename}
                onDragStart={onDragStart}
                onDragOver={onDragOver}
                onDrop={onDrop}
              />
            </div>
          )}
        </li>
      ))}
    </ul>
  )
}
