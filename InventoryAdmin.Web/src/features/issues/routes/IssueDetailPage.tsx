import { useParams, useNavigate } from 'react-router-dom'
import { PageHeader } from '@/shared/components/PageHeader'
import { Spinner } from '@/shared/components/Spinner'
import { useIssue, useAddIssueLine, useRemoveIssueLine, useAllocateIssueLineFefo, usePostIssue, useCancelIssue } from '../queries'
import { useConfirm } from '@/shared/components/confirm/ConfirmProvider'
import { swalPrompt } from '@/shared/utils/swal'

const statusLabels: Record<string, string> = {
  Draft: 'پیش‌نویس',
  Posted: 'ثبت شده',
  Canceled: 'لغو شده',
}

export function IssueDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { data: issue, isLoading } = useIssue(id)
  const addLine = useAddIssueLine(id!)
  const removeLine = useRemoveIssueLine(id!)
  const allocateFefo = useAllocateIssueLineFefo(id!)
  const post = usePostIssue(id!)
  const cancel = useCancelIssue(id!)
  const confirm = useConfirm()

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

  async function handleAllocateFefo(lineId: string) {
    const ok = await confirm.confirm({ title: 'تخصیص FEFO', message: 'آیا می‌خواهید این خط را با روش FEFO تخصیص دهید؟' })
    if (!ok) return
    await allocateFefo.mutateAsync(lineId)
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
              <span className="font-medium">شناسه انبار:</span> {issue.warehouseId}
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
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b">
                  <th className="text-right p-2">ردیف</th>
                  <th className="text-right p-2">محصول</th>
                  <th className="text-right p-2">واریانت</th>
                  <th className="text-right p-2">درخواستی</th>
                  <th className="text-right p-2">تخصیص یافته</th>
                  <th className="text-right p-2">باقیمانده</th>
                  <th className="text-right p-2">تخصیص‌ها</th>
                  {issue.status === 'Draft' && <th className="text-right p-2">عملیات</th>}
                </tr>
              </thead>
              <tbody>
                {issue.lines.map((line) => (
                  <tr key={line.id} className="border-b">
                    <td className="p-2">{line.lineNo}</td>
                    <td className="p-2">{line.productId.slice(0, 8)}...</td>
                    <td className="p-2">{line.variantId ? line.variantId.slice(0, 8) + '...' : '-'}</td>
                    <td className="p-2">{line.requestedQty}</td>
                    <td className="p-2">{line.allocatedQty}</td>
                    <td className="p-2">{line.remainingQty}</td>
                    <td className="p-2">
                      {line.allocations.length > 0 ? (
                        <span className="badge badge-green">{line.allocations.length}</span>
                      ) : (
                        <span className="badge badge-gray">0</span>
                      )}
                    </td>
                    {issue.status === 'Draft' && (
                      <td className="p-2">
                        <div className="flex gap-1">
                          <button onClick={() => handleAllocateFefo(line.id)} className="btn-secondary text-xs px-2 py-1">
                            FEFO
                          </button>
                          <button onClick={() => handleRemoveLine(line.id)} className="btn-red text-xs px-2 py-1">
                            حذف
                          </button>
                        </div>
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  )
}


