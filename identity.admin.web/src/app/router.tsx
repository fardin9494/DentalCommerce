import { createBrowserRouter, RouterProvider, Navigate } from 'react-router-dom'
import { Layout } from '../shared/components/Layout'
import { RouteError } from './RouteError'
import { IdentityAuthPage } from '../features/identity/routes/IdentityAuthPage'
import { IdentityMePage } from '../features/identity/routes/IdentityMePage'
import { IdentityTokensPage } from '../features/identity/routes/IdentityTokensPage'
import { IdentityUsersPage } from '../features/identity/routes/IdentityUsersPage'
import { IdentityUserDetailsPage } from '../features/identity/routes/IdentityUserDetailsPage'
import { IdentityReportsPage } from '../features/identity/routes/IdentityReportsPage'

const router = createBrowserRouter([
  {
    path: '/',
    element: <Layout />,
    errorElement: <RouteError />,
    children: [
      { index: true, element: <Navigate to="/identity/auth" replace /> },
      { path: 'identity/auth', element: <IdentityAuthPage /> },
      { path: 'identity/me', element: <IdentityMePage /> },
      { path: 'identity/users', element: <IdentityUsersPage /> },
      { path: 'identity/users/:id', element: <IdentityUserDetailsPage /> },
      { path: 'identity/tokens', element: <IdentityTokensPage /> },
      { path: 'identity/reports', element: <IdentityReportsPage /> },
    ],
  },
])

export function AppRouter() {
  return <RouterProvider router={router} />
}
