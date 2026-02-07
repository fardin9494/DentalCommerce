import { PropsWithChildren, useState } from 'react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ToastProvider } from '../shared/components/toast/ToastProvider'
import { ConfirmProvider } from '../shared/components/confirm/ConfirmProvider'
import { AdminAuthProvider } from './auth'
import { InventoryPermissionsProvider } from './permissions'

export function AppProviders({ children }: PropsWithChildren) {
  const [client] = useState(() => new QueryClient({
    defaultOptions: {
      queries: {
        retry: 1,
        refetchOnWindowFocus: false,
        staleTime: 20_000,
      },
    },
  }))

  return (
    <QueryClientProvider client={client}>
      <AdminAuthProvider>
        <InventoryPermissionsProvider>
          <ToastProvider>
            <ConfirmProvider>
              {children}
            </ConfirmProvider>
          </ToastProvider>
        </InventoryPermissionsProvider>
      </AdminAuthProvider>
    </QueryClientProvider>
  )
}




































