import { PropsWithChildren, createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import { fetchIdentityJson } from '@/lib/api/identityClient'
import { useAdminAuth } from './auth'

type PermissionUiPolicy = 'Disable' | 'Hide'

type PermissionItem = {
  key: string
  granted: boolean
}

type CatalogPermissionsResponse = {
  isSuperAdmin: boolean
  uiPolicy: PermissionUiPolicy
  items: PermissionItem[]
}

type PermissionsContextValue = {
  isLoading: boolean
  isSuperAdmin: boolean
  uiPolicy: PermissionUiPolicy
  can: (key: string) => boolean
  canAny: (keys: string[]) => boolean
  refresh: () => void
}

const PermissionsContext = createContext<PermissionsContextValue | undefined>(undefined)

export function CatalogPermissionsProvider({ children }: PropsWithChildren) {
  const { token } = useAdminAuth()
  const [data, setData] = useState<CatalogPermissionsResponse | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [refreshTick, setRefreshTick] = useState(0)

  const refresh = useCallback(() => setRefreshTick((v) => v + 1), [])

  useEffect(() => {
    let isActive = true

    if (!token) {
      setData(null)
      setIsLoading(false)
      return
    }

    setIsLoading(true)
    fetchIdentityJson<CatalogPermissionsResponse>('/me/permissions/catalog', { token })
      .then((resp) => {
        if (!isActive) return
        setData(resp)
      })
      .catch(() => {
        if (!isActive) return
        setData(null)
      })
      .finally(() => {
        if (!isActive) return
        setIsLoading(false)
      })

    return () => {
      isActive = false
    }
  }, [token, refreshTick])

  const grantedSet = useMemo(() => {
    if (!data?.items) return new Set<string>()
    return new Set(data.items.filter((x) => x.granted).map((x) => x.key))
  }, [data])

  const can = useCallback((key: string) => {
    if (!data || isLoading) return false
    if (data.isSuperAdmin) return true
    return grantedSet.has(key)
  }, [data, grantedSet, isLoading])

  const canAny = useCallback((keys: string[]) => {
    if (!data || isLoading) return false
    if (data.isSuperAdmin) return true
    return keys.some((k) => grantedSet.has(k))
  }, [data, grantedSet, isLoading])

  const value = useMemo<PermissionsContextValue>(() => ({
    isLoading,
    isSuperAdmin: data?.isSuperAdmin ?? false,
    uiPolicy: data?.uiPolicy ?? 'Disable',
    can,
    canAny,
    refresh,
  }), [isLoading, data, can, canAny, refresh])

  return <PermissionsContext.Provider value={value}>{children}</PermissionsContext.Provider>
}

export function useCatalogPermissions() {
  const ctx = useContext(PermissionsContext)
  if (!ctx) throw new Error('useCatalogPermissions must be used within CatalogPermissionsProvider')
  return ctx
}

type PermissionGateProps = PropsWithChildren<{
  permission?: string
  anyPermissions?: string[]
  policyOverride?: PermissionUiPolicy
}>

export function PermissionGate({ permission, anyPermissions, policyOverride, children }: PermissionGateProps) {
  const { can, canAny, uiPolicy, isLoading } = useCatalogPermissions()
  const policy = policyOverride ?? uiPolicy

  const allowed = isLoading
    ? false
    : permission
      ? can(permission)
      : anyPermissions
        ? canAny(anyPermissions)
        : true

  if (allowed) return <>{children}</>
  if (policy === 'Hide') return null

  return (
    <span className="pointer-events-none opacity-50">
      {children}
    </span>
  )
}
