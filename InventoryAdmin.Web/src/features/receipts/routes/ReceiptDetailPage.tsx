import { useParams, useNavigate } from 'react-router-dom'
import { PageHeader } from '@/shared/components/PageHeader'
import { Spinner } from '@/shared/components/Spinner'
import { useReceipt, useAddReceiptLine, useRemoveReceiptLine, useUpdateReceiptHeader, useUpdateReceiptLine, useReceiveReceipt, useApproveReceipt, useCancelReceipt } from '../queries'
import { useConfirm } from '@/shared/components/confirm/ConfirmProvider'
import { swalPrompt } from '@/shared/utils/swal'

const statusLabels: Record<string, string> = {
  Draft: 'پیش‌نویس',
  Received: 'دریافت شده',
  Approved: 'تایید شده',
  Canceled: 'لغو شده',
}

const reasonLabels: Record<string, string> = {
  Purchase: 'خرید',
  ReturnIn: 'مرجوعی',
  Production: 'تولید',
  Other: 'سایر',
}

export function ReceiptDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { data: receipt, isLoading } = useReceipt(id)
  const addLine = useAddReceiptLine(id!)
  const removeLine = useRemoveReceiptLine(id!)
  const updateHeader = useUpdateReceiptHeader(id!)
  const receive = useReceiveReceipt(id!)
  const approve = useApproveReceipt(id!)
  const cancel = useCancelReceipt(id!)
  const confirm = useConfirm()

  if (isLoading) return <Spinner />

  if (!receipt) {
    return (
      <div className="card p-4">
        <p className="text-red-600">رسید پیدا نشد</p>
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

    const lotNumber = await swalPrompt({ title: 'شماره لات - اختیاری', required: false })
    const expiryDate = await swalPrompt({ title: 'تاریخ انقضا (ISO) - اختیاری', required: false })
    const unitCostStr = await swalPrompt({ title: 'هزینه واحد - اختیاری', required: false })
    const unitCost = unitCostStr ? parseFloat(unitCostStr) : undefined

    await addLine.mutateAsync({
      productId,
      variantId: variantId || null,
      qty,
      lotNumber: lotNumber || null,
      expiryDateUtc: expiryDate || null,
      unitCost: unitCost || null,
    })
  }

  async function handleRemoveLine(lineId: string) {
    const ok = await confirm.confirm({ title: 'حذف خط', message: 'آیا از حذف این خط مطمئن هستید؟' })
    if (!ok) return
    await removeLine.mutateAsync(lineId)
  }

  async function handleUpdateHeader() {
    const externalRef = await swalPrompt({ title: 'ارجاع خارجی - اختیاری', required: false })
    const docDate = await swalPrompt({ title: 'تاریخ سند (ISO) - اختیاری', required: false })
    await updateHeader.mutateAsync({
      externalRef: externalRef || null,
      docDateUtc: docDate || null,
    })
  }

  async function handleReceive() {
    const ok = await confirm.confirm({ title: 'دریافت رسید', message: 'آیا می‌خواهید این رسید را دریافت کنید؟' })
    if (!ok) return
    await receive.mutateAsync()
  }

  async function handleApprove() {
    const ok = await confirm.confirm({ title: 'تایید رسید', message: 'آیا می‌خواهید این رسید را تایید کنید؟' })
    if (!ok) return
    await approve.mutateAsync()
  }

  async function handleCancel() {
    const ok = await confirm.confirm({ title: 'لغو رسید', message: 'آیا می‌خواهید این رسید را لغو کنید؟' })
    if (!ok) return
    await cancel.mutateAsync()
  }

  return (
    <div className="space-y-4">
      <PageHeader
        title={`رسید ${receipt.id.slice(0, 8)}...`}
        actions={
          <div className="flex gap-2">
            {receipt.status === 'Draft' && (
              <>
                <button onClick={handleAddLine} className="btn-secondary">افزودن خط</button>
                <button onClick={handleUpdateHeader} className="btn-secondary">ویرایش هدر</button>
                <button onClick={handleReceive} className="btn-green">دریافت</button>
                <button onClick={handleCancel} className="btn-red">لغو</button>
              </>
            )}
            {receipt.status === 'Received' && (
              <button onClick={handleApprove} className="btn-green">تایید</button>
            )}
            <button onClick={() => navigate('/receipts')} className="btn-secondary">بازگشت</button>
          </div>
        }
      >
        وضعیت: <span className="badge badge-blue">{statusLabels[receipt.status] || receipt.status}</span>
        {' • '}
        دلیل: <span className="badge badge-gray">{reasonLabels[receipt.reason] || receipt.reason}</span>
        {receipt.externalRef && ` • ارجاع: ${receipt.externalRef}`}
      </PageHeader>

      <div className="card p-4">
        <div className="space-y-2">
          <div className="grid grid-cols-2 gap-4 text-sm">
            <div>
              <span className="font-medium">شناسه انبار:</span> {receipt.warehouseId}
            </div>
            <div>
              <span className="font-medium">تاریخ سند:</span> {new Date(receipt.docDate).toLocaleString('fa-IR')}
            </div>
            {receipt.receivedAt && (
              <div>
                <span className="font-medium">تاریخ دریافت:</span> {new Date(receipt.receivedAt).toLocaleString('fa-IR')}
              </div>
            )}
            {receipt.approvedAt && (
              <div>
                <span className="font-medium">تاریخ تایید:</span> {new Date(receipt.approvedAt).toLocaleString('fa-IR')}
              </div>
            )}
          </div>
        </div>
      </div>

      <div className="card p-4">
        <h2 className="font-semibold mb-3">خطوط ({receipt.lines.length})</h2>
        {receipt.lines.length === 0 ? (
          <p className="text-sm text-gray-600">هیچ خطی وجود ندارد</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b">
                  <th className="text-right p-2">ردیف</th>
                  <th className="text-right p-2">محصول</th>
                  <th className="text-right p-2">واریانت</th>
                  <th className="text-right p-2">مقدار</th>
                  <th className="text-right p-2">لات</th>
                  <th className="text-right p-2">انقضا</th>
                  <th className="text-right p-2">هزینه واحد</th>
                  {receipt.status === 'Draft' && <th className="text-right p-2">عملیات</th>}
                </tr>
              </thead>
              <tbody>
                {receipt.lines.map((line) => (
                  <tr key={line.id} className="border-b">
                    <td className="p-2">{line.lineNo}</td>
                    <td className="p-2">{line.productId.slice(0, 8)}...</td>
                    <td className="p-2">{line.variantId ? line.variantId.slice(0, 8) + '...' : '-'}</td>
                    <td className="p-2">{line.qty}</td>
                    <td className="p-2">{line.lotNumber || '-'}</td>
                    <td className="p-2">{line.expiryDateUtc ? new Date(line.expiryDateUtc).toLocaleDateString('fa-IR') : '-'}</td>
                    <td className="p-2">{line.unitCost ? line.unitCost.toLocaleString('fa-IR') : '-'}</td>
                    {receipt.status === 'Draft' && (
                      <td className="p-2">
                        <button onClick={() => handleRemoveLine(line.id)} className="btn-red text-xs px-2 py-1">
                          حذف
                        </button>
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


