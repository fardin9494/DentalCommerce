import { useEffect, useMemo, useState, type FormEvent } from "react"
import { PageHeader } from "../../../shared/components/PageHeader"
import { Spinner } from "../../../shared/components/Spinner"
import { swalConfirm, swalToastError, swalToastInfo, swalToastSuccess } from "../../../shared/utils/swal"
import { getApiErrorMessage, getPricingPolicy, upsertPricingPolicy } from "../api"
import { listCatalogStores } from "../catalogApi"
import type { CatalogStoreListItem } from "../catalogTypes"
import { usePricingPolicies, usePricingPolicy, useUpsertPricingPolicy } from "../queries"
import type { PricingPolicy, StackingMode } from "../types"

type PolicyScope = "Global" | "Site"

type PolicyFormState = {
  scope: PolicyScope
  siteId: string
  defaultStackingMode: StackingMode
}

const stackingOptions: Array<{ value: StackingMode; label: string; help: string }> = [
  {
    value: "BestOfEachGroup",
    label: "بهترینِ هر گروه",
    help: "از هر گروه فقط بهترین کمپین انتخاب می‌شود (پیشنهاد پیش‌فرض برای جلوگیری از آشفتگی قیمت).",
  },
  {
    value: "BestPrice",
    label: "بهترین قیمت",
    help: "تنها کمپینی اعمال می‌شود که بیشترین کاهش قیمت را بدهد.",
  },
  {
    value: "PriorityOnly",
    label: "فقط اولویت",
    help: "فقط کمپین با بالاترین اولویت اعمال می‌شود.",
  },
  {
    value: "Cascading",
    label: "آبشاری",
    help: "کمپین‌ها به‌ترتیب اولویت و قابلیت ترکیب پشت سر هم اعمال می‌شوند.",
  },
]

const defaultMode: StackingMode = "BestOfEachGroup"

export function PricingPolicyPage() {
  const { data: globalPolicy, isLoading: isGlobalLoading } = usePricingPolicy()
  const { data: policies, isLoading: isPoliciesLoading } = usePricingPolicies()
  const upsert = useUpsertPricingPolicy()
  const [sitePolicies, setSitePolicies] = useState<Record<string, PricingPolicy>>({})

  const [stores, setStores] = useState<CatalogStoreListItem[]>([])
  const [storesLoading, setStoresLoading] = useState(false)

  const [modalOpen, setModalOpen] = useState(false)
  const [editing, setEditing] = useState<PricingPolicy | null>(null)
  const [form, setForm] = useState<PolicyFormState>({
    scope: "Global",
    siteId: "",
    defaultStackingMode: defaultMode,
  })

  const storeIndex = useMemo(() => new Map(stores.map(s => [s.id, s])), [stores])

  useEffect(() => {
    let active = true
    const loadStores = async () => {
      setStoresLoading(true)
      try {
        const data = await listCatalogStores()
        if (!active) return
        setStores(data || [])
      } catch (err) {
        if (!active) return
        swalToastError(getApiErrorMessage(err, "خطا در دریافت فهرست سایت‌ها از کاتالوگ."))
      } finally {
        if (active) setStoresLoading(false)
      }
    }
    void loadStores()
    return () => {
      active = false
    }
  }, [])

  useEffect(() => {
    if (!policies) return
    const next: Record<string, PricingPolicy> = {}
    policies.forEach(policy => {
      if (policy.siteId) next[policy.siteId] = policy
    })
    setSitePolicies(next)
  }, [policies])

  const openCreate = () => {
    setEditing(null)
    setForm({
      scope: "Global",
      siteId: "",
      defaultStackingMode: globalPolicy?.defaultStackingMode ?? defaultMode,
    })
    setModalOpen(true)
  }

  const openEdit = (policy: PricingPolicy) => {
    setEditing(policy)
    setForm({
      scope: policy.siteId ? "Site" : "Global",
      siteId: policy.siteId ?? "",
      defaultStackingMode: policy.defaultStackingMode,
    })
    setModalOpen(true)
  }

  const closeModal = () => {
    setModalOpen(false)
    setEditing(null)
  }

  const handleSave = async (e: FormEvent) => {
    e.preventDefault()
    if (form.scope === "Site" && !form.siteId.trim()) {
      swalToastError("سایت را انتخاب کنید.")
      return
    }

    const payload = {
      siteId: form.scope === "Site" ? form.siteId.trim() : null,
      defaultStackingMode: form.defaultStackingMode,
    }

    try {
      await upsert.mutateAsync(payload)
      if (payload.siteId) {
        setSitePolicies(prev => ({
          ...prev,
          [payload.siteId!]: {
            id: prev[payload.siteId!]?.id ?? "temp",
            siteId: payload.siteId,
            defaultStackingMode: payload.defaultStackingMode,
          },
        }))
      }
      swalToastSuccess("سیاست قیمت‌گذاری ذخیره شد.")
      closeModal()
    } catch (err) {
      swalToastError(getApiErrorMessage(err, "خطا در ذخیره سیاست قیمت‌گذاری."))
    }
  }

  const handleReset = async (policy: PricingPolicy) => {
    const ok = await swalConfirm({
      title: "بازنشانی سیاست",
      text: "با بازنشانی، سیاست به حالت پیش‌فرض سیستم برمی‌گردد. حذف واقعی وجود ندارد.",
      icon: "warning",
      confirmText: "بله",
      cancelText: "خیر",
    })
    if (!ok) return

    try {
      await upsertPricingPolicy({
        siteId: policy.siteId ?? null,
        defaultStackingMode: defaultMode,
      })
      if (policy.siteId) {
        setSitePolicies(prev => ({
          ...prev,
          [policy.siteId!]: { ...policy, defaultStackingMode: defaultMode },
        }))
      }
      swalToastSuccess("سیاست به حالت پیش‌فرض بازنشانی شد.")
    } catch (err) {
      swalToastError(getApiErrorMessage(err, "خطا در بازنشانی سیاست."))
    }
  }

  const handleLoadSitePolicy = async (siteId: string) => {
    if (!siteId.trim()) {
      swalToastError("سایت را انتخاب کنید.")
      return
    }
    try {
      const policy = await getPricingPolicy(siteId)
      if (!policy) {
        swalToastInfo("برای این سایت سیاستی یافت نشد.")
        return
      }
      setSitePolicies(prev => ({ ...prev, [siteId]: policy }))
      setForm(prev => ({ ...prev, scope: "Site", siteId, defaultStackingMode: policy.defaultStackingMode }))
    } catch (err) {
      swalToastError(getApiErrorMessage(err, "خطا در دریافت سیاست سایت."))
    }
  }

  const siteRows = useMemo(() => Object.values(sitePolicies), [sitePolicies])

  return (
    <div className="space-y-4">
      <PageHeader
        title="سیاست قیمت‌گذاری"
        actions={(
          <button className="btn" onClick={openCreate}>
            ایجاد سیاست جدید
          </button>
        )}
      >
        سیاست قیمت‌گذاری تعیین می‌کند وقتی چند کمپین واجد شرایط شدند، کدام ترکیب اعمال شود. سیاست سایت
        همیشه بر سیاست سراسری مقدم است.
      </PageHeader>

      <div className="card p-4">
        <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
          <div>
            <div className="font-semibold">سیاست سراسری</div>
            <div className="text-xs text-gray-600">
              اگر برای سایت خاص سیاستی تعریف نشده باشد، از سیاست سراسری استفاده می‌شود.
            </div>
          </div>
          <button className="btn-secondary px-4 py-2 rounded" onClick={() => openEdit(globalPolicy ?? { id: "global", siteId: null, defaultStackingMode: globalPolicy?.defaultStackingMode ?? defaultMode })}>
            {globalPolicy ? "ویرایش" : "تعریف سیاست"}
          </button>
        </div>
        <div className="mt-3 grid grid-cols-1 md:grid-cols-2 gap-3">
          <div className="rounded-lg border border-slate-200 bg-slate-50 px-3 py-2">
            <div className="text-xs text-gray-600">حالت پیش‌فرض</div>
            <div className="font-semibold">
              {stackingOptions.find(o => o.value === (globalPolicy?.defaultStackingMode ?? defaultMode))?.label}
            </div>
          </div>
          <div className="rounded-lg border border-slate-200 bg-slate-50 px-3 py-2">
            <div className="text-xs text-gray-600">وضعیت سیاست</div>
            <div className="font-semibold">{globalPolicy ? "تعریف شده" : "تعریف نشده (پیش‌فرض سیستم)"}</div>
          </div>
        </div>
      </div>

      <div className="card p-4 space-y-3">
        <div className="flex items-center justify-between">
          <h3 className="font-semibold">سیاست‌های سایت</h3>
          <button className="btn-secondary px-3 py-1.5 rounded" onClick={openCreate}>افزودن سیاست سایت</button>
        </div>
        {isGlobalLoading || isPoliciesLoading ? <Spinner /> : (
          <div className="overflow-x-auto">
            <table className="min-w-full text-sm text-center">
              <thead>
                <tr className="bg-gray-100">
                  <th className="p-2">سایت</th>
                  <th className="p-2">حالت انباشت</th>
                  <th className="p-2">توضیح</th>
                  <th className="p-2">عملیات</th>
                </tr>
              </thead>
              <tbody>
                {siteRows.length === 0 && (
                  <tr className="border-b last:border-0">
                    <td className="p-3 text-sm text-gray-600" colSpan={4}>سیاستی برای سایت‌ها بارگذاری نشده است.</td>
                  </tr>
                )}
                {siteRows.map(p => (
                  <tr key={p.siteId ?? p.id} className="border-b last:border-0">
                    <td className="p-2">
                      {p.siteId ? (storeIndex.get(p.siteId)?.name ?? p.siteId) : "-"}
                    </td>
                    <td className="p-2">
                      {stackingOptions.find(o => o.value === p.defaultStackingMode)?.label ?? p.defaultStackingMode}
                    </td>
                    <td className="p-2 text-right">
                      {stackingOptions.find(o => o.value === p.defaultStackingMode)?.help ?? "-"}
                    </td>
                    <td className="p-2">
                      <div className="flex items-center justify-center gap-2">
                        <button className="btn-secondary px-3 py-1.5 rounded" onClick={() => openEdit(p)}>ویرایش</button>
                        <button className="btn-red px-3 py-1.5 rounded" onClick={() => handleReset(p)}>بازنشانی</button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {modalOpen && (
        <PolicyModal
          editing={editing}
          form={form}
          setForm={setForm}
          stores={stores}
          storesLoading={storesLoading}
          onClose={closeModal}
          onSubmit={handleSave}
          onLoadSitePolicy={handleLoadSitePolicy}
          isSaving={upsert.isPending}
        />
      )}
    </div>
  )
}

function PolicyModal(props: {
  editing: PricingPolicy | null
  form: PolicyFormState
  setForm: (next: PolicyFormState | ((prev: PolicyFormState) => PolicyFormState)) => void
  stores: CatalogStoreListItem[]
  storesLoading: boolean
  onClose: () => void
  onSubmit: (e: FormEvent) => void
  onLoadSitePolicy: (siteId: string) => void
  isSaving: boolean
}) {
  const [storeSearch, setStoreSearch] = useState("")

  const filteredStores = useMemo(() => {
    const term = storeSearch.trim().toLowerCase()
    if (!term) return props.stores
    return props.stores.filter(s => {
      const name = s.name?.toLowerCase() ?? ""
      const domain = s.domain?.toLowerCase() ?? ""
      return name.includes(term) || domain.includes(term)
    })
  }, [props.stores, storeSearch])

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={props.onClose} />
      <div className="relative w-[96vw] max-w-4xl max-h-[90vh] rounded-2xl bg-white shadow-xl flex flex-col overflow-hidden">
        <div className="flex items-center justify-between border-b px-6 py-4">
          <div>
            <div className="text-lg font-semibold">
              {props.editing ? "ویرایش سیاست قیمت‌گذاری" : "ایجاد سیاست قیمت‌گذاری"}
            </div>
            <div className="text-xs text-gray-500">
              سیاست سایت بر سیاست سراسری مقدم است و فقط روی همان سایت اثر دارد.
            </div>
          </div>
          <button className="btn-secondary px-4 py-2.5 rounded-lg" onClick={props.onClose}>بستن</button>
        </div>

        <form className="p-6 space-y-4 overflow-y-auto" onSubmit={props.onSubmit}>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="label">نوع سیاست</label>
              <select
                className="input"
                value={props.form.scope}
                onChange={e => props.setForm(prev => ({ ...prev, scope: e.target.value as PolicyScope, siteId: "" }))}
                disabled={!!props.editing}
              >
                <option value="Global">سراسری (Global)</option>
                <option value="Site">سایت</option>
              </select>
            </div>

            {props.form.scope === "Site" && (
              <div>
                <label className="label">انتخاب سایت</label>
                <input
                  className="input mb-2"
                  placeholder="جستجو در سایت‌ها..."
                  value={storeSearch}
                  onChange={e => setStoreSearch(e.target.value)}
                  disabled={props.storesLoading || props.stores.length === 0}
                />
                <select
                  className="input"
                  value={props.form.siteId}
                  onChange={e => props.setForm(prev => ({ ...prev, siteId: e.target.value }))}
                  disabled={props.storesLoading || props.stores.length === 0 || !!props.editing}
                >
                  <option value="">انتخاب کنید</option>
                  {filteredStores.map(s => (
                    <option key={s.id} value={s.id}>{s.name}</option>
                  ))}
                </select>
                {props.stores.length === 0 && (
                  <div className="text-xs text-amber-600 mt-2">فهرست سایت‌ها دریافت نشد.</div>
                )}
                <div className="mt-2">
                  <button
                    type="button"
                    className="btn-secondary px-3 py-1.5 rounded"
                    onClick={() => props.onLoadSitePolicy(props.form.siteId)}
                    disabled={!props.form.siteId}
                  >
                    بارگذاری سیاست موجود
                  </button>
                </div>
              </div>
            )}
          </div>

          <div>
            <label className="label">حالت انباشت کمپین‌ها</label>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              {stackingOptions.map(opt => {
                const active = props.form.defaultStackingMode === opt.value
                return (
                  <button
                    key={opt.value}
                    type="button"
                    className={`text-right rounded-lg border px-3 py-2 transition ${active ? "border-emerald-500 bg-emerald-50" : "border-slate-200 hover:border-slate-300"}`}
                    onClick={() => props.setForm(prev => ({ ...prev, defaultStackingMode: opt.value }))}
                  >
                    <div className="font-semibold text-sm">{opt.label}</div>
                    <div className="text-xs text-gray-600 mt-1">{opt.help}</div>
                  </button>
                )
              })}
            </div>
          </div>

          <div className="rounded-lg border border-slate-200 bg-slate-50 p-3 text-xs text-gray-700">
            اگر سیاست سایت تعریف شود، آن سیاست جایگزین سیاست سراسری می‌شود. در غیر این صورت، سیستم از حالت
            پیش‌فرض استفاده می‌کند.
          </div>

          <div className="flex items-center gap-2 justify-end">
            <button type="button" className="btn-secondary px-4 py-2 rounded-lg" onClick={props.onClose} disabled={props.isSaving}>
              انصراف
            </button>
            <button type="submit" className="btn px-5 py-2 rounded-lg" disabled={props.isSaving}>
              {props.isSaving ? "در حال ذخیره..." : "ذخیره"}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
