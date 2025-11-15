import { PropsWithChildren, ReactNode } from 'react'

type Props = PropsWithChildren<{
  title: string
  actions?: ReactNode
}>

export function PageHeader({ title, actions, children }: Props) {
  return (
    <div className="mb-4">
      <div className="flex items-center justify-between gap-2">
        <h1 className="text-lg font-semibold">{title}</h1>
        {actions}
      </div>
      {children ? <div className="mt-2 text-sm text-gray-600">{children}</div> : null}
    </div>
  )
}

