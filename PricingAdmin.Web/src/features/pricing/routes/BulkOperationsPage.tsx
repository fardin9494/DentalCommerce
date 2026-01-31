import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { PageHeader } from '../../../shared/components/PageHeader'
import { swalToastError, swalToastSuccess } from '../../../shared/utils/swal'
import { JalaliDateTimePicker } from '../../../shared/components/JalaliDateTimePicker'
import { getApiErrorMessage } from '../api'
import { useBulkCreateCategoryCampaign, useBulkUpdatePrices, usePriceLists, usePriceList } from '../queries'
import type { BenefitDefinition, StackingMode } from '../types'
import { SkuPicker } from '../components/SkuPicker'
import { listCatalogCategoryLeaves } from '../catalogApi'
import type { CatalogCategoryLeaf } from '../catalogTypes'

type BulkCampaignForm = {
  categoryIds: string
  namePrefix: string
  isActive: boolean
  validFrom: string
  validTo: string
  priority: string
  stackingGroup: string
  stackingMode: StackingMode
  combinableWithOtherPromotions: boolean
  combinableWithCoupons: boolean
  exclusiveGroup: string
  maxDiscountPercent: string
  maxDiscountAmount: string
  benefitType: 'percentOff' | 'amountOff' | 'fixedPrice'
  benefitValue: string
  benefitCurrency: string
}

const emptyCampaign = (): BulkCampaignForm => ({
  categoryIds: '',
  namePrefix: '',
  isActive: true,
  validFrom: '',
  validTo: '',
  priority: '0',
  stackingGroup: 'Category',
  stackingMode: 'BestOfEachGroup',
  combinableWithOtherPromotions: true,
  combinableWithCoupons: true,
  exclusiveGroup: '',
  maxDiscountPercent: '',
  maxDiscountAmount: '',
  benefitType: 'percentOff',
  benefitValue: '',
  benefitCurrency: 'IRR',
})

export function BulkOperationsPage() {
  const [campaignForm, setCampaignForm] = useState<BulkCampaignForm>(() => emptyCampaign())
  const [catalogCategories, setCatalogCategories] = useState<CatalogCategoryLeaf[]>([])
  const [categoriesLoading, setCategoriesLoading] = useState(false)
  const [categorySearch, setCategorySearch] = useState('')
  const [categoryPick, setCategoryPick] = useState('')
  const [previewCampaign, setPreviewCampaign] = useState<string[]>([])

  const { data: priceLists } = usePriceLists()
  const bulkCampaign = useBulkCreateCategoryCampaign()

  const categoryList = useMemo(() => parseList(campaignForm.categoryIds), [campaignForm.categoryIds])
  const previewCampaignHead = useMemo(() => previewCampaign.slice(0, 6), [previewCampaign])
  const categoryMap = useMemo(() => new Map(catalogCategories.map(c => [c.id, c])), [catalogCategories])
  const filteredCategories = useMemo(() => {
    const query = categorySearch.trim().toLowerCase()
    const selected = new Set(categoryList)
    return catalogCategories.filter(category => {
      if (selected.has(category.id)) return false
      if (!query) return true
      return category.name.toLowerCase().includes(query) || category.slug.toLowerCase().includes(query)
    })
  }, [catalogCategories, categorySearch, categoryList])

  useEffect(() => {
    let active = true
    const loadCategories = async () => {
      setCategoriesLoading(true)
      try {
        const categories = await listCatalogCategoryLeaves()
        if (!active) return
        setCatalogCategories(categories || [])
      } catch (err) {
        if (!active) return
        swalToastError('خطا در دریافت دسته‌بندی‌ها از کاتالوگ.')
      } finally {
        if (active) setCategoriesLoading(false)
      }
    }
    void loadCategories()
    return () => {
      active = false
    }
  }, [])

  const handleCampaignPreview = () => {
    if (categoryList.length === 0) {
      swalToastError('حداقل یک دسته‌بندی انتخاب کنید.')
      return
    }
    setPreviewCampaign(categoryList)
  }

  const handleCampaignSubmit = async (e: FormEvent) => {
    e.preventDefault()
    if (categoryList.length === 0) {
      swalToastError('حداقل یک دسته‌بندی انتخاب کنید.')
      return
    }
    if (!campaignForm.namePrefix.trim()) {
      swalToastError('پیشوند نام لازم است.')
      return
    }
    const benefit = buildSimpleBenefit(campaignForm)
    if (!benefit) return
    try {
      const res = await bulkCampaign.mutateAsync({
        categoryIds: categoryList,
        categories: categoryList.map((id) => ({
          id,
          name: categoryMap.get(id)?.name ?? id,
        })),
        namePrefix: campaignForm.namePrefix.trim(),
        isActive: campaignForm.isActive,
        validFrom: campaignForm.validFrom || null,
        validTo: campaignForm.validTo || null,
        priority: Number.parseInt(campaignForm.priority || '0', 10) || 0,
        stackingGroup: campaignForm.stackingGroup.trim() || 'Category',
        stackingMode: campaignForm.stackingMode,
        combinableWithOtherPromotions: campaignForm.combinableWithOtherPromotions,
        combinableWithCoupons: campaignForm.combinableWithCoupons,
        exclusiveGroup: campaignForm.exclusiveGroup.trim() || null,
        guardrails: buildGuardrails(campaignForm.maxDiscountPercent, campaignForm.maxDiscountAmount),
        benefit,
      })
      swalToastSuccess('کمپین‌های ایجاد شده: ' + res.created)
    } catch (err) {
      swalToastError(getApiErrorMessage(err, 'خطا در ایجاد کمپین‌ها.'))
    }
  }

  const handleAddCategory = (categoryId: string) => {
    if (!categoryId) return
    setCampaignForm(f => ({ ...f, categoryIds: addCategoryId(f.categoryIds, categoryId) }))
  }

  const handleRemoveCategory = (categoryId: string) => {
    setCampaignForm(f => ({ ...f, categoryIds: removeCategoryId(f.categoryIds, categoryId) }))
  }

  return (
    <div className="space-y-4">
      <PageHeader title="عملیات گروهی">
        تغییر قیمت گروهی محصولات و ایجاد کمپین تخفیف برای دسته‌بندی‌ها.
      </PageHeader>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        <div className="card p-4 space-y-3">
          <h3 className="font-semibold">تغییر قیمت گروهی محصولات</h3>
          <div className="rounded-md border border-slate-200 bg-slate-50 p-3 text-xs text-gray-700">
            مرحله 1: محصولات را انتخاب کنید. مرحله 2: نوع عملیات و مقدار را مشخص کنید. مرحله 3: اعمال و ثبت کنید.
          </div>
          <BulkPriceAdjustmentSection priceLists={priceLists ?? []} />
        </div>

        <div className="card p-4 space-y-3">
          <h3 className="font-semibold">کمپین تخفیف برای دسته‌بندی‌ها</h3>
          <div className="rounded-md border border-slate-200 bg-slate-50 p-3 text-xs text-gray-700">
            این بخش به‌ازای هر دسته‌بندی انتخاب‌شده، یک کمپین ایجاد می‌کند (با نام = پیشوند + نام دسته). تنظیمات پیشرفته اختیاری است.
          </div>
          <form className="space-y-3" onSubmit={handleCampaignSubmit}>
            <div>
              <label className="label">انتخاب دسته‌بندی‌ها</label>
              <div className="border rounded-md p-3 space-y-2">
                <div className="grid grid-cols-1 md:grid-cols-3 gap-2">
                  <input
                    className="input"
                    value={categorySearch}
                    onChange={e => setCategorySearch(e.target.value)}
                    placeholder="جستجو بر اساس نام یا اسلاگ"
                  />
                  <select
                    className="input"
                    value={categoryPick}
                    onChange={e => setCategoryPick(e.target.value)}
                    disabled={categoriesLoading}
                  >
                    <option value="">{categoriesLoading ? 'در حال دریافت دسته‌ها...' : 'انتخاب دسته'}</option>
                    {filteredCategories.map(category => (
                      <option key={category.id} value={category.id}>
                        {formatCategoryOption(category)}
                      </option>
                    ))}
                  </select>
                  <button
                    type="button"
                    className="btn-secondary px-3 py-2 rounded"
                    onClick={() => {
                      if (!categoryPick) return
                      handleAddCategory(categoryPick)
                      setCategoryPick('')
                    }}
                    disabled={!categoryPick}
                  >
                    افزودن
                  </button>
                </div>
                {categoryList.length > 0 && (
                  <div className="flex flex-wrap gap-2">
                    {categoryList.map(id => {
                      const category = categoryMap.get(id)
                      const label = category ? formatCategoryOption(category) : id
                      return (
                        <span key={id} className="badge badge-gray flex items-center gap-2">
                          <span>{label}</span>
                          <button type="button" className="text-red-600" onClick={() => handleRemoveCategory(id)}>×</button>
                        </span>
                      )
                    })}
                  </div>
                )}
                <div className="text-xs text-gray-600">
                  دسته‌بندی‌های انتخاب‌شده: {categoryList.length.toLocaleString('fa-IR')}
                </div>
                <details className="text-xs text-gray-600">
                  <summary className="cursor-pointer">ورود دستی شناسه‌ها (اختیاری)</summary>
                  <textarea
                    className="input mt-2 min-h-[100px]"
                    value={campaignForm.categoryIds}
                    onChange={e => setCampaignForm(f => ({ ...f, categoryIds: e.target.value }))}
                  />
                </details>
              </div>
              <div className="mt-2 flex items-center justify-between text-xs text-gray-600">
                <span>تعداد دسته‌ها: {categoryList.length.toLocaleString('fa-IR')}</span>
                <button
                  type="button"
                  className="btn-secondary px-2 py-1 rounded"
                  onClick={() => {
                    setCampaignForm(f => ({ ...f, categoryIds: '' }))
                    setPreviewCampaign([])
                  }}
                >
                  پاک کردن
                </button>
              </div>
            </div>
            <div>
              <label className="label">پیشوند نام</label>
              <input className="input" value={campaignForm.namePrefix} onChange={e => setCampaignForm(f => ({ ...f, namePrefix: e.target.value }))} />
              <div className="mt-1 text-xs text-gray-600">
                مثال: اگر «تخفیف زمستان» بزنید، برای هر دسته یک کمپین مثل «تخفیف زمستان - {`{نام دسته}` }» ساخته می‌شود.
              </div>
            </div>
            <label className="flex items-center gap-2 text-sm">
              <input type="checkbox" checked={campaignForm.isActive} onChange={e => setCampaignForm(f => ({ ...f, isActive: e.target.checked }))} />
              فعال باشد
            </label>

            <div className="border rounded-md p-3 space-y-2">
              <h4 className="font-semibold">مزیت</h4>
              <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
                <div>
                  <label className="label">نوع</label>
                  <select className="input" value={campaignForm.benefitType} onChange={e => setCampaignForm(f => ({ ...f, benefitType: e.target.value as BulkCampaignForm['benefitType'] }))}>
                    <option value="percentOff">درصد تخفیف</option>
                    <option value="amountOff">مبلغ تخفیف</option>
                    <option value="fixedPrice">قیمت ثابت</option>
                  </select>
                </div>
                <div>
                  <label className="label">مقدار</label>
                  <input className="input" type="number" value={campaignForm.benefitValue} onChange={e => setCampaignForm(f => ({ ...f, benefitValue: e.target.value }))} />
                </div>
                {(campaignForm.benefitType === 'amountOff' || campaignForm.benefitType === 'fixedPrice') ? (
                  <div>
                    <label className="label">ارز</label>
                    <input className="input" value={campaignForm.benefitCurrency} onChange={e => setCampaignForm(f => ({ ...f, benefitCurrency: e.target.value }))} />
                  </div>
                ) : (
                  <div className="hidden md:block" />
                )}
              </div>
            </div>

            <details className="border rounded-md p-3">
              <summary className="cursor-pointer text-sm font-semibold">تنظیمات پیشرفته (اختیاری)</summary>
              <div className="mt-3 space-y-3">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                  <div>
                    <label className="label">شروع</label>
                    <JalaliDateTimePicker value={campaignForm.validFrom} onChange={(v) => setCampaignForm(f => ({ ...f, validFrom: v }))} clearable />
                  </div>
                  <div>
                    <label className="label">پایان</label>
                    <JalaliDateTimePicker value={campaignForm.validTo} onChange={(v) => setCampaignForm(f => ({ ...f, validTo: v }))} clearable />
                  </div>
                </div>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                  <div>
                    <label className="label">اولویت</label>
                    <input className="input" type="number" value={campaignForm.priority} onChange={e => setCampaignForm(f => ({ ...f, priority: e.target.value }))} />
                  </div>
                  <div>
                    <label className="label">گروه انباشت</label>
                    <input className="input" value={campaignForm.stackingGroup} onChange={e => setCampaignForm(f => ({ ...f, stackingGroup: e.target.value }))} />
                    <div className="mt-1 text-xs text-gray-600">اگر خالی باشد، پیش‌فرض «Category» استفاده می‌شود.</div>
                  </div>
                </div>
                <div>
                  <label className="label">حالت انباشت</label>
                  <select className="input" value={campaignForm.stackingMode} onChange={e => setCampaignForm(f => ({ ...f, stackingMode: e.target.value as StackingMode }))}>
                    <option value="BestOfEachGroup">بهترین از هر گروه</option>
                    <option value="BestPrice">بهترین قیمت</option>
                    <option value="PriorityOnly">فقط اولویت</option>
                    <option value="Cascading">آبشاری</option>
                  </select>
                </div>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                  <label className="flex items-center gap-2 text-sm">
                    <input type="checkbox" checked={campaignForm.combinableWithOtherPromotions} onChange={e => setCampaignForm(f => ({ ...f, combinableWithOtherPromotions: e.target.checked }))} />
                    قابل ترکیب با کمپین‌ها
                  </label>
                  <label className="flex items-center gap-2 text-sm">
                    <input type="checkbox" checked={campaignForm.combinableWithCoupons} onChange={e => setCampaignForm(f => ({ ...f, combinableWithCoupons: e.target.checked }))} />
                    قابل ترکیب با کوپن‌ها
                  </label>
                </div>
                <div>
                  <label className="label">گروه انحصاری (اختیاری)</label>
                  <input className="input" value={campaignForm.exclusiveGroup} onChange={e => setCampaignForm(f => ({ ...f, exclusiveGroup: e.target.value }))} />
                  <div className="mt-1 text-xs text-gray-600">اگر چند کمپین یک «گروه انحصاری» داشته باشند، فقط یکی با اولویت بالاتر اعمال می‌شود.</div>
                </div>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                  <div>
                    <label className="label">حداکثر درصد تخفیف</label>
                    <input className="input" type="number" value={campaignForm.maxDiscountPercent} onChange={e => setCampaignForm(f => ({ ...f, maxDiscountPercent: e.target.value }))} />
                  </div>
                  <div>
                    <label className="label">حداکثر مبلغ تخفیف</label>
                    <input className="input" type="number" value={campaignForm.maxDiscountAmount} onChange={e => setCampaignForm(f => ({ ...f, maxDiscountAmount: e.target.value }))} />
                  </div>
                </div>
              </div>
            </details>

            <div className="flex items-center gap-2">
              <button type="button" className="btn-secondary px-3 py-1.5 rounded" onClick={handleCampaignPreview}>
                پیش‌نمایش
              </button>
              <button type="submit" className="btn px-3 py-1.5 rounded" disabled={bulkCampaign.isPending}>
                {bulkCampaign.isPending ? 'در حال ایجاد...' : 'ایجاد'}
              </button>
            </div>
          </form>

          {previewCampaign.length > 0 && (
            <div className="mt-3 rounded-md border border-slate-200 bg-slate-50 p-3 text-xs text-gray-700">
              <div className="font-semibold">پیش‌نمایش دسته‌ها</div>
              <div className="mt-2 flex flex-wrap gap-2">
                {previewCampaignHead.map(id => (
                  <span key={id} className="badge badge-gray font-mono">{id}</span>
                ))}
                {previewCampaign.length > previewCampaignHead.length && (
                  <span className="text-gray-600">و {previewCampaign.length - previewCampaignHead.length} مورد دیگر</span>
                )}
              </div>
              <div className="mt-2 text-gray-600">تعداد کل: {previewCampaign.length.toLocaleString('fa-IR')}</div>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}

function parseList(input: string) {
  return input.split(/\r?\n|,/).map(s => s.trim()).filter(Boolean)
}

function addCategoryId(input: string, categoryId: string) {
  const list = parseList(input)
  if (!list.includes(categoryId)) list.push(categoryId)
  return list.join('\n')
}

function removeCategoryId(input: string, categoryId: string) {
  const list = parseList(input).filter(id => id !== categoryId)
  return list.join('\n')
}

function formatCategoryOption(category: CatalogCategoryLeaf) {
  const indent = category.depth > 0 ? '-'.repeat(category.depth) + ' ' : ''
  return `${indent}${category.name} (${category.slug})`
}

function buildSimpleBenefit(form: BulkCampaignForm): BenefitDefinition | null {
  const value = Number.parseFloat(form.benefitValue || '0')
  if (!Number.isFinite(value)) {
    swalToastError('مقدار مزیت معتبر وارد کنید.')
    return null
  }
  if (form.benefitType === 'percentOff') {
    return { kind: 'percentOff', percent: value }
  }
  if (form.benefitType === 'amountOff') {
    return { kind: 'amountOff', amount: value, currency: form.benefitCurrency || null }
  }
  return { kind: 'fixedPrice', price: value, currency: form.benefitCurrency || null }
}

function buildGuardrails(percent: string, amount: string) {
  const p = percent ? Number.parseFloat(percent) : NaN
  const a = amount ? Number.parseFloat(amount) : NaN
  if (!Number.isFinite(p) && !Number.isFinite(a)) return null
  return {
    maxDiscountPercent: Number.isFinite(p) ? p : null,
    maxDiscountAmount: Number.isFinite(a) ? a : null,
  }
}

function roundByCurrency(value: number, currency: string) {
  const c = (currency || '').toUpperCase()
  if (c === 'IRR' || c === 'IRT') return Math.round(value)
  return Math.round(value * 100) / 100
}

type SelectedSku = {
  skuId: string
  label: string
  currentPrice?: number
  newPrice?: number
}

function BulkPriceAdjustmentSection({ priceLists }: { priceLists: Array<{ id: string; name: string; currency: string; isActive: boolean }> }) {
  const [selectedPriceListId, setSelectedPriceListId] = useState('')
  const [selectedSkus, setSelectedSkus] = useState<SelectedSku[]>([])
  const [operationType, setOperationType] = useState<'percent' | 'amount' | 'fixed'>('percent')
  const [operationValue, setOperationValue] = useState('')
  const [previewItems, setPreviewItems] = useState<Array<{ skuId: string; basePrice: number }>>([])
  const [skuToAdd, setSkuToAdd] = useState('')
  
  const { data: priceList } = usePriceList(selectedPriceListId || undefined)
  const bulkPrices = useBulkUpdatePrices(selectedPriceListId || undefined)

  const priceListMap = useMemo(() => {
    if (!priceList) return new Map<string, number>()
    const map = new Map<string, number>()
    for (const item of priceList.items || []) {
      map.set(item.skuId.toLowerCase(), item.basePrice)
    }
    return map
  }, [priceList])

  // Update current prices when price list changes
  useEffect(() => {
    if (priceListMap.size > 0 && selectedSkus.length > 0) {
      setSelectedSkus(prev => prev.map(sku => {
        const currentPrice = priceListMap.get(sku.skuId.toLowerCase())
        return { ...sku, currentPrice }
      }))
    }
  }, [priceListMap])

  const handleAddSku = (next: { skuId: string; label: string }) => {
    if (!next.skuId) return
    const normalized = next.skuId.toLowerCase()
    if (selectedSkus.some(s => s.skuId.toLowerCase() === normalized)) {
      swalToastError('این محصول قبلاً اضافه شده است.')
      return
    }
    const currentPrice = priceListMap.get(normalized)
    setSelectedSkus(prev => [...prev, { skuId: next.skuId, label: next.label, currentPrice }])
    setSkuToAdd('')
  }

  const handleRemoveSku = (skuId: string) => {
    setSelectedSkus(prev => prev.filter(s => s.skuId !== skuId))
    setPreviewItems([])
  }

  const handleCalculatePreview = () => {
    if (selectedSkus.length === 0) {
      swalToastError('حداقل یک محصول انتخاب کنید.')
      return
    }
    if (!selectedPriceListId) {
      swalToastError('لیست قیمت را انتخاب کنید.')
      return
    }
    const value = Number.parseFloat(operationValue)
    if (!Number.isFinite(value)) {
      swalToastError('مقدار عملیات معتبر وارد کنید.')
      return
    }

    const currency = priceList?.currency || 'IRR'
    const calculated = selectedSkus.map(sku => {
      const current = sku.currentPrice ?? 0
      let newPrice = current

      if (operationType === 'percent') {
        newPrice = current * (1 + value / 100)
      } else if (operationType === 'amount') {
        newPrice = current + value
      } else if (operationType === 'fixed') {
        newPrice = value
      }

      newPrice = roundByCurrency(Math.max(0, newPrice), currency)
      
      return {
        skuId: sku.skuId,
        basePrice: newPrice,
      }
    })

    setPreviewItems(calculated)
    setSelectedSkus(prev => prev.map(s => {
      const calculatedItem = calculated.find(c => c.skuId === s.skuId)
      return { ...s, newPrice: calculatedItem?.basePrice }
    }))
  }

  const handleSubmit = async () => {
    if (previewItems.length === 0) {
      swalToastError('ابتدا پیش‌نمایش را انجام دهید.')
      return
    }
    if (!selectedPriceListId) {
      swalToastError('لیست قیمت را انتخاب کنید.')
      return
    }
    try {
      const currency = priceList?.currency || 'IRR'
      const res = await bulkPrices.mutateAsync({
        priceListId: selectedPriceListId || null,
        currency,
        items: previewItems,
      })
      swalToastSuccess(`قیمت ${res.updated} محصول به‌روزرسانی شد.`)
      setSelectedSkus([])
      setPreviewItems([])
      setOperationValue('')
    } catch (err) {
      swalToastError(getApiErrorMessage(err, 'خطا در ثبت تغییرات قیمت.'))
    }
  }

  const activePriceList = priceLists.find(l => l.isActive) || priceLists[0]
  const defaultPriceListId = selectedPriceListId || activePriceList?.id || ''

  return (
    <div className="space-y-3">
      <div>
        <label className="label">لیست قیمت</label>
        <select
          className="input"
          value={selectedPriceListId}
          onChange={e => {
            setSelectedPriceListId(e.target.value)
            setSelectedSkus([])
            setPreviewItems([])
          }}
        >
          <option value="">انتخاب لیست قیمت</option>
          {priceLists.map(l => (
            <option key={l.id} value={l.id}>
              {l.name} ({l.currency}){l.isActive ? ' - فعال' : ''}
            </option>
          ))}
        </select>
      </div>

      <div>
        <label className="label">انتخاب محصولات</label>
        <div className="border rounded-md p-3 space-y-2">
          <SkuPicker
            label="جستجو و افزودن محصول"
            value={skuToAdd}
            onChange={handleAddSku}
            showLabel={false}
          />
          {selectedSkus.length > 0 && (
            <div className="space-y-2">
              <div className="text-xs text-gray-600">محصولات انتخاب شده: {selectedSkus.length}</div>
              <div className="max-h-48 overflow-y-auto border rounded-md p-2 space-y-1">
                {selectedSkus.map(sku => (
                  <div key={sku.skuId} className="flex items-center justify-between p-2 bg-gray-50 rounded text-sm">
                    <div className="min-w-0 flex-1">
                      <div className="font-semibold truncate">{sku.label || sku.skuId}</div>
                      <div className="text-xs text-gray-600">
                        SKU: {sku.skuId}
                        {sku.currentPrice !== undefined && (
                          <span className="mr-2"> | قیمت فعلی: {sku.currentPrice.toLocaleString('fa-IR')}</span>
                        )}
                        {sku.newPrice !== undefined && (
                          <span className="mr-2 text-green-600"> | قیمت جدید: {sku.newPrice.toLocaleString('fa-IR')}</span>
                        )}
                      </div>
                    </div>
                    <button
                      type="button"
                      className="btn-secondary px-2 py-1 rounded text-xs"
                      onClick={() => handleRemoveSku(sku.skuId)}
                    >
                      حذف
                    </button>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      </div>

      <div className="border rounded-md p-3 space-y-2 bg-slate-50">
        <div className="font-semibold text-sm">عملیات قیمتی</div>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-2">
          <div>
            <label className="label">نوع عملیات</label>
            <select
              className="input"
              value={operationType}
              onChange={e => {
                setOperationType(e.target.value as 'percent' | 'amount' | 'fixed')
                setPreviewItems([])
              }}
            >
              <option value="percent">تغییر درصدی</option>
              <option value="amount">تغییر ریالی</option>
              <option value="fixed">قیمت ثابت</option>
            </select>
          </div>
          <div>
            <label className="label">مقدار</label>
            <input
              className="input"
              type="number"
              value={operationValue}
              onChange={e => {
                setOperationValue(e.target.value)
                setPreviewItems([])
              }}
              placeholder={
                operationType === 'percent'
                  ? 'مثال: 10 (افزایش 10%) یا -10 (کاهش 10%)'
                  : operationType === 'amount'
                  ? 'مثال: 5000 (افزایش) یا -5000 (کاهش)'
                  : 'مثال: 100000 (قیمت ثابت)'
              }
            />
          </div>
          <div className="flex items-end">
            <button
              type="button"
              className="btn-secondary px-3 py-2 rounded w-full"
              onClick={handleCalculatePreview}
              disabled={selectedSkus.length === 0 || !selectedPriceListId}
            >
              محاسبه
            </button>
          </div>
        </div>
        <div className="text-xs text-gray-600">
          {operationType === 'percent' && 'مقدار درصد تغییر را وارد کنید (مثبت برای افزایش، منفی برای کاهش)'}
          {operationType === 'amount' && 'مقدار ریالی تغییر را وارد کنید (مثبت برای افزایش، منفی برای کاهش)'}
          {operationType === 'fixed' && 'قیمت ثابت جدید را برای همه محصولات وارد کنید'}
        </div>
      </div>

      {previewItems.length > 0 && (
        <div className="border rounded-md p-3 space-y-2">
          <div className="font-semibold text-sm">پیش‌نمایش تغییرات</div>
          <div className="overflow-x-auto max-h-64">
            <table className="min-w-full text-xs text-center">
              <thead>
                <tr className="bg-gray-100">
                  <th className="p-2">محصول</th>
                  <th className="p-2">قیمت فعلی</th>
                  <th className="p-2">قیمت جدید</th>
                  <th className="p-2">تغییر</th>
                </tr>
              </thead>
              <tbody>
                {previewItems.map((item, idx) => {
                  const sku = selectedSkus.find(s => s.skuId === item.skuId)
                  const current = sku?.currentPrice ?? 0
                  const change = item.basePrice - current
                  const changePercent = current > 0 ? ((change / current) * 100).toFixed(1) : '0'
                  return (
                    <tr key={`${item.skuId}-${idx}`} className="border-b last:border-0">
                      <td className="p-2 text-right">
                        <div className="font-semibold">{sku?.label || item.skuId}</div>
                        <div className="text-xs text-gray-600">{item.skuId}</div>
                      </td>
                      <td className="p-2">{current.toLocaleString('fa-IR')}</td>
                      <td className="p-2 font-semibold text-green-600">{item.basePrice.toLocaleString('fa-IR')}</td>
                      <td className="p-2">
                        <span className={change >= 0 ? 'text-green-600' : 'text-red-600'}>
                          {change >= 0 ? '+' : ''}{change.toLocaleString('fa-IR')} ({changePercent}%)
                        </span>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        </div>
      )}

      <div className="flex items-center gap-2">
        <button
          className="btn px-3 py-1.5 rounded"
          onClick={handleSubmit}
          disabled={bulkPrices.isPending || previewItems.length === 0}
        >
          {bulkPrices.isPending ? 'در حال ثبت...' : 'ثبت تغییرات'}
        </button>
        <button
          type="button"
          className="btn-secondary px-3 py-1.5 rounded"
          onClick={() => {
            setSelectedSkus([])
            setPreviewItems([])
            setOperationValue('')
          }}
        >
          پاک کردن همه
        </button>
      </div>
    </div>
  )
}
