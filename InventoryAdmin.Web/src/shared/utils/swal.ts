// SweetAlert2 thin wrappers with RTL-friendly defaults
// Uses global Swal (loaded via CDN in index.html). Falls back to window.alert/confirm if missing.

declare global {
  interface Window { Swal?: any }
}

function getSwal() {
  return typeof window !== 'undefined' ? (window.Swal || null) : null
}

export async function swalConfirm(opts: { title?: string; text?: string; confirmText?: string; cancelText?: string; icon?: 'question'|'warning'|'info'|'error'|'success' }) {
  const Swal = getSwal()
  if (!Swal) return Promise.resolve(window.confirm(opts.text || opts.title || 'Are you sure?'))
  const res = await Swal.fire({
    title: opts.title || 'تأیید عملیات',
    text: opts.text || '',
    icon: opts.icon || 'question',
    showCancelButton: true,
    confirmButtonText: opts.confirmText || 'بله',
    cancelButtonText: opts.cancelText || 'خیر',
    reverseButtons: true,
  })
  return !!res.isConfirmed
}

export function swalToastSuccess(message: string, ms = 2500) {
  const Swal = getSwal()
  if (!Swal) return window.alert(message)
  const Toast = Swal.mixin({ toast: true, position: 'top', showConfirmButton: false, timer: ms, timerProgressBar: true })
  return Toast.fire({ icon: 'success', title: message })
}

export function swalToastError(message: string, ms = 3000) {
  const Swal = getSwal()
  if (!Swal) return window.alert(message)
  // If message is long or multi-line, show a modal instead of a toast
  const isLong = typeof message === 'string' && (message.length > 160 || /\n|\r/.test(message))
  if (isLong) {
    const html = `<pre dir="ltr" style="text-align:left;white-space:pre-wrap;max-height:60vh;overflow:auto;padding:8px;background:#f9fafb;border-radius:6px;">${escapeHtml(message)}</pre>`
    return Swal.fire({ icon: 'error', title: 'خطا', html, width: '60em', confirmButtonText: 'باشه', focusConfirm: true })
  }
  const Toast = Swal.mixin({ toast: true, position: 'top', showConfirmButton: false, timer: ms, timerProgressBar: true })
  return Toast.fire({ icon: 'error', title: message })
}

export function swalToastInfo(message: string, ms = 2500) {
  const Swal = getSwal()
  if (!Swal) return window.alert(message)
  const Toast = Swal.mixin({ toast: true, position: 'top', showConfirmButton: false, timer: ms, timerProgressBar: true })
  return Toast.fire({ icon: 'info', title: message })
}

function escapeHtml(s: string) {
  return s
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;')
}

// Text input prompt
export async function swalPrompt(opts: { title?: string; inputLabel?: string; placeholder?: string; defaultValue?: string; confirmText?: string; cancelText?: string; required?: boolean; textarea?: boolean }) {
  const Swal = getSwal()
  if (!Swal) {
    const v = window.prompt(opts.title || '', opts.defaultValue || '')
    if (opts.required && !(v && v.trim())) return null
    return v ?? null
  }
  const res = await Swal.fire({
    title: opts.title || '',
    input: opts.textarea ? 'textarea' : 'text',
    inputLabel: opts.inputLabel,
    inputPlaceholder: opts.placeholder,
    inputValue: opts.defaultValue || '',
    showCancelButton: true,
    confirmButtonText: opts.confirmText || 'تایید',
    cancelButtonText: opts.cancelText || 'انصراف',
    reverseButtons: true,
    inputValidator: (value: string) => (opts.required && !String(value || '').trim() ? 'مقدار لازم است' : undefined)
  })
  return res.isConfirmed ? (res.value as string) : null
}


