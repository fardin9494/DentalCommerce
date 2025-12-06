import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { PageHeader } from '@/shared/components/PageHeader'
import { Spinner } from '@/shared/components/Spinner'
import { useCreateReceipt } from '../queries'
import { useToast } from '@/shared/components/toast/ToastProvider'
import { swalPrompt } from '@/shared/utils/swal'

export function ReceiptsListPage() {
  const navigate = useNavigate()
  const createReceipt = useCreateReceipt()
  const toast = useToast()
  const [creating, setCreating] = useState(false)

  async function handleCreateReceipt() {
    const warehouseId = await swalPrompt({
      title: 'ایجاد رسید جدید',
      inputLabel: 'شناسه انبار (UUID)',
      placeholder: '00000000-0000-0000-0000-000000000000',
      required: true,
    })
    if (!warehouseId) return

    const reasonStr = await swalPrompt({
      title: 'دلیل ورود',
      inputLabel: 'دلیل (1=Purchase, 2=ReturnIn, 3=Production, 99=Other)',
      placeholder: '1',
      defaultValue: '1',
      required: true,
    })
    if (!reasonStr) return

    const reason = parseInt(reasonStr, 10)
    if (isNaN(reason) || reason < 1 || (reason > 3 && reason !== 99)) {
      toast.error('دلیل نامعتبر است')
      return
    }

    setCreating(true)
    try {
      const result = await createReceipt.mutateAsync({
        warehouseId,
        reason,
        externalRef: null,
        docDateUtc: new Date().toISOString(),
      })
      navigate(`/receipts/${result.id}`)
    } catch (err: any) {
      // Error handled by mutation
    } finally {
      setCreating(false)
    }
  }

  return (
    <div className="space-y-4">
      <PageHeader
        title="رسیدها"
        actions={
          <button onClick={handleCreateReceipt} disabled={creating} className="btn">
            {creating ? 'در حال ایجاد...' : 'ایجاد رسید جدید'}
          </button>
        }
      >
        مدیریت رسیدهای ورود به انبار
      </PageHeader>

      <div className="card p-4">
        <p className="text-sm text-gray-600">
          برای مشاهده یا ایجاد رسید، از دکمه بالا استفاده کنید. لیست کامل رسیدها به زودی اضافه خواهد شد.
        </p>
      </div>
    </div>
  )
}


