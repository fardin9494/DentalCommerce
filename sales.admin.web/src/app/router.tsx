import { createBrowserRouter, RouterProvider, Navigate } from 'react-router-dom'
import { Layout } from '../shared/components/Layout'
import { RouteError } from './RouteError'
import { SalesTestPage } from '../features/sales/routes/SalesTestPage'
import { OrdersListPage } from '../features/sales/routes/OrdersListPage'
import { OrderDetailsPage } from '../features/sales/routes/OrderDetailsPage'
import { SalesReportsPage } from '../features/sales/routes/SalesReportsPage'
import { PermissionGate } from './permissions'
import { SalesPermissionKeys } from './salesPermissionKeys'

const router = createBrowserRouter([
  {
    path: '/',
    element: <Layout />,
    errorElement: <RouteError />,
    children: [
      { index: true, element: <Navigate to="/sales/orders" replace /> },
      {
        path: 'sales/orders',
        element: (
          <PermissionGate permission={SalesPermissionKeys.OrdersView}>
            <OrdersListPage />
          </PermissionGate>
        ),
      },
      {
        path: 'sales/orders/:id',
        element: (
          <PermissionGate permission={SalesPermissionKeys.OrdersDetailView}>
            <OrderDetailsPage />
          </PermissionGate>
        ),
      },
      {
        path: 'sales/reports',
        element: (
          <PermissionGate anyPermissions={[
            SalesPermissionKeys.ReportsDailyView,
            SalesPermissionKeys.ReportsMonthlyView,
            SalesPermissionKeys.ReportsBySiteView,
            SalesPermissionKeys.ReportsByProductView,
            SalesPermissionKeys.ReportsByCustomerView,
          ]}>
            <SalesReportsPage />
          </PermissionGate>
        ),
      },
      { path: 'sales/test', element: <SalesTestPage /> },
    ],
  },
])

export function AppRouter() {
  return <RouterProvider router={router} />
}
