import { PropsWithChildren, createContext, useContext, useEffect, useState } from 'react'

type AuthContextValue = {
  token: string | null
  setToken: (token: string | null) => void
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

export function AdminAuthProvider({ children }: PropsWithChildren) {
  const [token, setToken] = useState<string | null>(() => {
    if (typeof window === 'undefined') return null
    try {
      return localStorage.getItem('admin_token')
    } catch {
      return null
    }
  })

  useEffect(() => {
    if (typeof window === 'undefined') return
    try {
      if (token) localStorage.setItem('admin_token', token)
      else localStorage.removeItem('admin_token')
    } catch {
      // ignore storage errors
    }
  }, [token])

  const logout = () => setToken(null)

  return (
    <AuthContext.Provider value={{ token, setToken, logout }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAdminAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAdminAuth must be used within AdminAuthProvider')
  return ctx
}


