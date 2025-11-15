import React, { createContext, useCallback, useContext, useMemo, useRef, useState } from 'react'
import { createPortal } from 'react-dom'

type Toast = {
  id: number
  type: 'success' | 'error' | 'info'
  message: string
  duration?: number
}

type ToastApi = {
  show: (message: string, opts?: { type?: Toast['type']; duration?: number }) => void
  success: (message: string, duration?: number) => void
  error: (message: string, duration?: number) => void
  info: (message: string, duration?: number) => void
}

const ToastCtx = createContext<ToastApi | null>(null)

export function useToast() {
  const api = useContext(ToastCtx)
  if (!api) throw new Error('useToast must be used within ToastProvider')
  return api
}

export function ToastProvider({ children }: { children: React.ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([])
  const idRef = useRef(1)

  const remove = useCallback((id: number) => {
    setToasts(ts => ts.filter(t => t.id !== id))
  }, [])

  const show = useCallback((message: string, opts?: { type?: Toast['type']; duration?: number }) => {
    const id = idRef.current++
    const t: Toast = { id, type: opts?.type || 'info', message, duration: opts?.duration ?? 3000 }
    setToasts(ts => [...ts, t])
    if (t.duration && t.duration > 0) {
      setTimeout(() => remove(id), t.duration)
    }
  }, [remove])

  const api: ToastApi = useMemo(() => ({
    show,
    success: (m, d) => show(m, { type: 'success', duration: d }),
    error: (m, d) => show(m, { type: 'error', duration: d }),
    info: (m, d) => show(m, { type: 'info', duration: d }),
  }), [show])

  return (
    <ToastCtx.Provider value={api}>
      {children}
      {createPortal(
        <div className="fixed top-4 left-1/2 -translate-x-1/2 z-50 space-y-2 w-[90%] max-w-sm">
          {toasts.map(t => (
            <div key={t.id}
                 className={`rounded px-3 py-2 shadow text-sm text-white ${t.type==='success' ? 'bg-emerald-600' : t.type==='error' ? 'bg-rose-600' : 'bg-gray-800'}`}
                 dir="rtl">
              {t.message}
            </div>
          ))}
        </div>,
        document.body
      )}
    </ToastCtx.Provider>
  )
}

