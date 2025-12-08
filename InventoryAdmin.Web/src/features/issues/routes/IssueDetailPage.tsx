import { useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { PageHeader } from '@/shared/components/PageHeader'
import { Spinner } from '@/shared/components/Spinner'
import { useIssue, useAddIssueLine, useRemoveIssueLine, useAllocateIssueLineFefo, useAllocateIssueLineFifo, useAllocateIssueLineLifo, usePostIssue, useCancelIssue } from '../queries'
import { useConfirm } from '@/shared/components/confirm/ConfirmProvider'
import { swalPrompt } from '@/shared/utils/swal'
import { AllocateMethodModal } from '../components/AllocateMethodModal'
import { useActiveWarehouses } from '@/shared/hooks/useWarehouses'

const statusLabels: Record<string, string> = {
  Draft: 'پیش‌نویس',
  Posted: 'ثبت شده',
  Canceled: 'لغو شده',
}

export function IssueDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { data: issue, isLoading } = useIssue(id)
  const { data: warehouses } = useActiveWarehouses()
  const addLine = useAddIssueLine(id!)
  const removeLine = useRemoveIssueLine(id!)
  const allocateFefo = useAllocateIssueLineFefo(id!)
  const allocateFifo = useAllocateIssueLineFifo(id!)
  const allocateLifo = useAllocateIssueLineLifo(id!)
  const post = usePostIssue(id!)
  const cancel = useCancelIssue(id!)
  const confirm = useConfirm()
  const [allocateModalOpen, setAllocateModalOpen] = useState(false)
  const [selectedLineId, setSelectedLineId] = useState<string | null>(null)
  const [expandedLineId, setExpandedLineId] = useState<string | null>(null)

  if (isLoading) return <Spinner />

  if (!issue) {
    return (
      <div className="card p-4">
        <p className="text-red-600">خروجی پیدا نشد</p>
      </div>
    )
  }

  async function handleAddLine() {
    const productId = await swalPrompt({ title: 'شناسه محصول (UUID)', required: true })
    if (!productId) return
    const variantId = await swalPrompt({ title: 'شناسه واریانت (UUID) - اختیاری', required: false })
    const qtyStr = await swalPrompt({ title: 'مقدار', placeholder: '1', defaultValue: '1', required: true })
    if (!qtyStr) return
    const qty = parseFloat(qtyStr)
    if (isNaN(qty) || qty <= 0) return

    await addLine.mutateAsync({
      productId,
      variantId: variantId || null,
      qty,
    })
  }

  async function handleRemoveLine(lineId: string) {
    const ok = await confirm.confirm({ title: 'حذف خط', message: 'آیا از حذف این خط مطمئن هستید؟' })
    if (!ok) return
    await removeLine.mutateAsync(lineId)
  }

  function handleOpenAllocateModal(lineId: string) {
    setSelectedLineId(lineId)
    setAllocateModalOpen(true)
  }

  async function handleAllocate(method: 'fefo' | 'fifo' | 'lifo', preferredWarehouseId?: string) {
    if (!selectedLineId) return
    
    const line = issue?.lines.find(l => l.id === selectedLineId)
    if (!line) return

    const methodLabels = {
      fefo: 'FEFO',
      fifo: 'FIFO',
      lifo: 'LIFO',
    }

    const warehouseName = preferredWarehouseId 
      ? warehouses?.find(w => w.id === preferredWarehouseId)?.name 
      : 'همه انبارها'

    const ok = await confirm.confirm({
      title: `تخصیص ${methodLabels[method]}`,
      message: `آیا می‌خواهید خط ${line.lineNo} را با روش ${methodLabels[method]} ${preferredWarehouseId ? `از انبار ${warehouseName}` : 'از تمام انبارها'} تخصیص دهید؟`
    })
    if (!ok) return

    try {
      switch (method) {
        case 'fefo':
          await allocateFefo.mutateAsync({ lineId: selectedLineId, preferredWarehouseId })
          break
        case 'fifo':
          await allocateFifo.mutateAsync({ lineId: selectedLineId, preferredWarehouseId })
          break
        case 'lifo':
          await allocateLifo.mutateAsync({ lineId: selectedLineId, preferredWarehouseId })
          break
      }
    } finally {
      setAllocateModalOpen(false)
      setSelectedLineId(null)
    }
  }

  async function handlePost() {
    const ok = await confirm.confirm({ title: 'ثبت خروجی', message: 'آیا می‌خواهید این خروجی را ثبت کنید؟' })
    if (!ok) return
    await post.mutateAsync()
  }

  async function handleCancel() {
    const ok = await confirm.confirm({ title: 'لغو خروجی', message: 'آیا می‌خواهید این خروجی را لغو کنید؟' })
    if (!ok) return
    await cancel.mutateAsync()
  }

  return (
    <div className="space-y-4">
      <PageHeader
        title={`خروجی ${issue.id.slice(0, 8)}...`}
        actions={
          <div className="flex gap-2">
            {issue.status === 'Draft' && (
              <>
                <button onClick={handleAddLine} className="btn-secondary">افزودن خط</button>
                <button onClick={handlePost} className="btn-green">ثبت</button>
                <button onClick={handleCancel} className="btn-red">لغو</button>
              </>
            )}
            <button onClick={() => navigate('/issues')} className="btn-secondary">بازگشت</button>
          </div>
        }
      >
        وضعیت: <span className="badge badge-blue">{statusLabels[issue.status] || issue.status}</span>
        {issue.externalRef && ` • ارجاع: ${issue.externalRef}`}
      </PageHeader>

      <div className="card p-4">
        <div className="space-y-2">
          <div className="grid grid-cols-2 gap-4 text-sm">
            <div>
              <span className="font-medium">انبار:</span>{' '}
              {warehouses?.find((w) => w.id === issue.warehouseId)?.name || issue.warehouseId.slice(0, 8) + '...'}
            </div>
            <div>
              <span className="font-medium">تاریخ سند:</span> {new Date(issue.docDate).toLocaleString('fa-IR')}
            </div>
            {issue.postedAt && (
              <div>
                <span className="font-medium">تاریخ ثبت:</span> {new Date(issue.postedAt).toLocaleString('fa-IR')}
              </div>
            )}
          </div>
        </div>
      </div>

      <div className="card p-4">
        <h2 className="font-semibold mb-3">خطوط ({issue.lines.length})</h2>
        {issue.lines.length === 0 ? (
          <p className="text-sm text-gray-600">هیچ خطی وجود ندارد</p>
        ) : (
          <div className="space-y-4">
            {issue.lines.map((line) => (
              <div key={line.id} className="border rounded-lg overflow-hidden">
                {/* Line Header */}
                <div className="bg-gray-50 p-3 border-b">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-4 text-sm">
                      <span className="font-semibold">خط {line.lineNo}</span>
                      <span className="text-gray-600">
                        محصول: {line.productId.slice(0, 8)}...
                        {line.variantId && ` | واریانت: ${line.variantId.slice(0, 8)}...`}
                      </span>
                      <span className="text-gray-600">
                        درخواستی: <span className="font-medium">{line.requestedQty.toLocaleString('fa-IR')}</span>
                      </span>
                      <span className="text-gray-600">
                        تخصیص یافته: <span className="font-medium text-green-600">{line.allocatedQty.toLocaleString('fa-IR')}</span>
                      </span>
                      <span className="text-gray-600">
                        باقیمانده: <span className={`font-medium ${line.remainingQty > 0 ? 'text-orange-600' : 'text-green-600'}`}>
                          {line.remainingQty.toLocaleString('fa-IR')}
                        </span>
                      </span>
                    </div>
                    <div className="flex items-center gap-2">
                      {line.allocations.length > 0 && (
                        <button
                          onClick={() => setExpandedLineId(expandedLineId === line.id ? null : line.id)}
                          className="text-xs text-blue-600 hover:text-blue-800"
                        >
                          {expandedLineId === line.id ? 'بستن جزئیات' : `مشاهده ${line.allocations.length} تخصیص`}
                        </button>
                      )}
                      {issue.status === 'Draft' && (
                        <>
                          <button
                            onClick={() => handleOpenAllocateModal(line.id)}
                            className="btn-secondary text-xs px-2 py-1"
                          >
                            تخصیص
                          </button>
                          <button onClick={() => handleRemoveLine(line.id)} className="btn-red text-xs px-2 py-1">
                            حذف
                          </button>
                        </>
                      )}
                    </div>
                  </div>
                </div>

                {/* Allocations Details */}
                {expandedLineId === line.id && line.allocations.length > 0 && (
                  <div className="p-4 bg-white">
                    <h4 className="font-medium mb-3 text-sm">جزئیات تخصیص‌ها:</h4>
                    <div className="overflow-x-auto">
                      <table className="w-full text-xs">
                        <thead>
                          <tr className="border-b bg-gray-50">
                            <th className="text-right p-2">SKU</th>
                            <th className="text-right p-2">شماره لات</th>
                            <th className="text-right p-2">تاریخ انقضا</th>
                            <th className="text-right p-2">قفسه</th>
                            <th className="text-right p-2">مقدار</th>
                          </tr>
                        </thead>
                        <tbody>
                          {line.allocations.map((alloc) => (
                            <tr key={alloc.id} className="border-b">
                              <td className="p-2 font-mono">{alloc.sku || '-'}</td>
                              <td className="p-2">{alloc.lotNumber || '-'}</td>
                              <td className="p-2">
                                {alloc.expiryDate ? new Date(alloc.expiryDate).toLocaleDateString('fa-IR') : '-'}
                              </td>
                              <td className="p-2">{alloc.shelfName || '-'}</td>
                              <td className="p-2 font-medium">{alloc.qty.toLocaleString('fa-IR')}</td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  </div>
                )}
              </div>
            ))}
          </div>
        )}
      </div>

      {selectedLineId && (
        <AllocateMethodModal
          isOpen={allocateModalOpen}
          onClose={() => {
            setAllocateModalOpen(false)
            setSelectedLineId(null)
          }}
          onSelect={handleAllocate}
          lineNo={issue?.lines.find(l => l.id === selectedLineId)?.lineNo || 0}
          isAllocating={allocateFefo.isPending || allocateFifo.isPending || allocateLifo.isPending}
          defaultWarehouseId={issue?.warehouseId}
        />
      )}
    </div>
  )
}




