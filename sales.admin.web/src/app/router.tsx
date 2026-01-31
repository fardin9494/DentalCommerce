import { createBrowserRouter, RouterProvider, Navigate } from 'react-router-dom'
import { Layout } from '../shared/components/Layout'
import { RouteError } from './RouteError'
import { SalesTestPage } from '../features/sales/routes/SalesTestPage'
import { OrdersListPage } from '../features/sales/routes/OrdersListPage'
import { OrderDetailsPage } from '../features/sales/routes/OrderDetailsPage'
import { SalesReportsPage } from '../features/sales/routes/SalesReportsPage'

const router = createBrowserRouter([
  {
    path: '/',
    element: <Layout />,
    errorElement: <RouteError />,
    children: [
      { index: true, element: <Navigate to="/sales/orders" replace /> },
      { path: 'sales/orders', element: <OrdersListPage /> },
      { path: 'sales/orders/:id', element: <OrderDetailsPage /> },
      { path: 'sales/reports', element: <SalesReportsPage /> },
      { path: 'sales/test', element: <SalesTestPage /> },
    ],
  },
])

export function AppRouter() {
  return <RouterProvider router={router} />
}
