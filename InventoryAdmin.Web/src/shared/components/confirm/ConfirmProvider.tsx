import React, { createContext, useContext, useMemo, useState } from 'react'
import { createPortal } from 'react-dom'

type ConfirmOptions = {
  title?: string
  message?: string
  confirmText?: string
  cancelText?: string
}

type ConfirmApi = {
  confirm: (opts: ConfirmOptions) => Promise<boolean>
}

const Ctx = createContext<ConfirmApi | null>(null)

export function useConfirm() {
  const api = useContext(Ctx)
  if (!api) throw new Error('useConfirm must be used within ConfirmProvider')
  return api
}

export function ConfirmProvider({ children }: { children: React.ReactNode }) {
  const [state, setState] = useState<{ open: boolean; opts: ConfirmOptions; resolve?: (v: boolean)=>void }>({ open: false, opts: {} })

  const api: ConfirmApi = useMemo(() => ({
    confirm: (opts) => new Promise<boolean>(resolve => setState({ open: true, opts, resolve }))
  }), [])

  const close = (v: boolean) => {
    const res = state.resolve
    setState({ open: false, opts: {} })
    res?.(v)
  }

  return (
    <Ctx.Provider value={api}>
      {children}
      {state.open && createPortal(
        <div className="fixed inset-0 z-50 flex items-center justify-center">
          <div className="absolute inset-0 bg-black/40" onClick={()=>close(false)} />
          <div className="relative bg-white rounded shadow-lg w-[90%] max-w-md p-4" dir="rtl">
            <h3 className="font-semibold mb-2">{state.opts.title || 'تأیید عملیات'}</h3>
            {state.opts.message && <p className="text-sm text-gray-700 mb-3">{state.opts.message}</p>}
            <div className="flex justify-end gap-2">
              <button className="btn-secondary px-3 py-1.5 rounded" onClick={()=>close(false)}>{state.opts.cancelText || 'خیر'}</button>
              <button className="btn px-3 py-1.5 rounded" onClick={()=>close(true)}>{state.opts.confirmText || 'بله'}</button>
            </div>
          </div>
        </div>,
        document.body
      )}
    </Ctx.Provider>
  )
}


