type Props = { value: string; onChange: (html: string) => void }

export function DescriptionEditor({ value, onChange }: Props) {
  return (
    <div>
      <div className="label mb-1">توضیحات محصول</div>
      <textarea
        className="input h-40"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder="توضیحات محصول را وارد کنید"
      />
    </div>
  )
}
