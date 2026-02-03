import { PropsWithChildren, createContext, useContext, useEffect, useState } from 'react'

type AuthContextValue = {
  accessToken: string | null
  refreshToken: string | null
  siteId: string | null
  userId: string | null
  setAuth: (data: {
    accessToken: string | null
    refreshToken?: string | null
    siteId?: string | null
    userId?: string | null
  }) => void
  clear: () => void
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

export function AdminAuthProvider({ children }: PropsWithChildren) {
  const [accessToken, setAccessToken] = useState<string | null>(() => {
    if (typeof window === 'undefined') return null
    try {
      return localStorage.getItem('identity_access_token')
    } catch {
      return null
    }
  })
  const [refreshToken, setRefreshToken] = useState<string | null>(() => {
    if (typeof window === 'undefined') return null
    try {
      return localStorage.getItem('identity_refresh_token')
    } catch {
      return null
    }
  })
  const [siteId, setSiteId] = useState<string | null>(() => {
    if (typeof window === 'undefined') return null
    try {
      return localStorage.getItem('identity_site_id')
    } catch {
      return null
    }
  })
  const [userId, setUserId] = useState<string | null>(() => {
    if (typeof window === 'undefined') return null
    try {
      return localStorage.getItem('identity_user_id')
    } catch {
      return null
    }
  })

  useEffect(() => {
    if (typeof window === 'undefined') return
    try {
      if (accessToken) localStorage.setItem('identity_access_token', accessToken)
      else localStorage.removeItem('identity_access_token')
    } catch {
      // ignore storage errors
    }
  }, [accessToken])

  useEffect(() => {
    if (typeof window === 'undefined') return
    try {
      if (refreshToken) localStorage.setItem('identity_refresh_token', refreshToken)
      else localStorage.removeItem('identity_refresh_token')
    } catch {}
  }, [refreshToken])

  useEffect(() => {
    if (typeof window === 'undefined') return
    try {
      if (siteId) localStorage.setItem('identity_site_id', siteId)
      else localStorage.removeItem('identity_site_id')
    } catch {}
  }, [siteId])

  useEffect(() => {
    if (typeof window === 'undefined') return
    try {
      if (userId) localStorage.setItem('identity_user_id', userId)
      else localStorage.removeItem('identity_user_id')
    } catch {}
  }, [userId])

  const setAuth = (data: {
    accessToken: string | null
    refreshToken?: string | null
    siteId?: string | null
    userId?: string | null
  }) => {
    setAccessToken(data.accessToken ?? null)
    if (data.refreshToken !== undefined) setRefreshToken(data.refreshToken ?? null)
    if (data.siteId !== undefined) setSiteId(data.siteId ?? null)
    if (data.userId !== undefined) setUserId(data.userId ?? null)
  }

  const clear = () => {
    setAccessToken(null)
    setRefreshToken(null)
    setSiteId(null)
    setUserId(null)
  }

  return (
    <AuthContext.Provider value={{ accessToken, refreshToken, siteId, userId, setAuth, clear }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAdminAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAdminAuth must be used within AdminAuthProvider')
  return ctx
}
