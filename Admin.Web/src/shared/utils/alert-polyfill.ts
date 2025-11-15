// Upgrade window.alert to SweetAlert2 toast if available
declare global { interface Window { Swal?: any } }

const Swal = typeof window !== 'undefined' ? (window as any).Swal : undefined

// Patch alert -> toast
if (typeof window !== 'undefined' && typeof window.alert === 'function') {
  const originalAlert = window.alert.bind(window)
  window.alert = (msg?: any) => {
    try {
      const text = typeof msg === 'string' ? msg : String(msg)
      if (text === '__CANCELLED__') return
      if (Swal) {
        const isLong = text.length > 160 || /\n|\r/.test(text)
        if (isLong) {
          const html = `<pre dir="ltr" style="text-align:left;white-space:pre-wrap;max-height:60vh;overflow:auto;padding:8px;background:#f9fafb;border-radius:6px;">${text.replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;')}</pre>`
          Swal.fire({ icon: 'info', title: 'پیام', html, width: '60em', confirmButtonText: 'باشه' })
        } else {
          const Toast = Swal.mixin({ toast: true, position: 'top', showConfirmButton: false, timer: 2500, timerProgressBar: true })
          Toast.fire({ icon: 'info', title: text })
        }
      } else {
        originalAlert(msg)
      }
    } catch {
      originalAlert(msg)
    }
  }
}

// Do NOT override prompt() globally to avoid breaking synchronous flows.
// Use swalPrompt helper (async) at call sites instead.

// Suppress native confirm dialogs globally to avoid double-modals.
// Real confirmations are handled via SweetAlert2 in mutation hooks/components.
if (typeof window !== 'undefined' && typeof window.confirm === 'function') {
  window.confirm = (_?: any) => true
}
