import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { PageHeader } from '@/shared/components/PageHeader'
import { useCreateIssue } from '../queries'
import { swalPrompt } from '@/shared/utils/swal'

export function IssuesListPage() {
  const navigate = useNavigate()
  const createIssue = useCreateIssue()
  const [creating, setCreating] = useState(false)

  async function handleCreateIssue() {
    const warehouseId = await swalPrompt({
      title: 'ایجاد خروجی جدید',
      inputLabel: 'شناسه انبار (UUID)',
      placeholder: '00000000-0000-0000-0000-000000000000',
      required: true,
    })
    if (!warehouseId) return

    setCreating(true)
    try {
      const result = await createIssue.mutateAsync({
        warehouseId,
        externalRef: null,
        docDateUtc: new Date().toISOString(),
      })
      navigate(`/issues/${result.id}`)
    } catch (err: any) {
      // Error handled by mutation
    } finally {
      setCreating(false)
    }
  }

  return (
    <div className="space-y-4">
      <PageHeader
        title="خروجی‌ها"
        actions={
          <button onClick={handleCreateIssue} disabled={creating} className="btn">
            {creating ? 'در حال ایجاد...' : 'ایجاد خروجی جدید'}
          </button>
        }
      >
        مدیریت خروجی‌های انبار
      </PageHeader>

      <div className="card p-4">
        <p className="text-sm text-gray-600">
          برای مشاهده یا ایجاد خروجی، از دکمه بالا استفاده کنید. لیست کامل خروجی‌ها به زودی اضافه خواهد شد.
        </p>
      </div>
    </div>
  )
}


