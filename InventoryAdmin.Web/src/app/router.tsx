import { createBrowserRouter, RouterProvider, Navigate } from 'react-router-dom'
import { Layout } from '../shared/components/Layout'
import { RouteError } from './RouteError'
import { ReceiptsListPage } from '../features/receipts/routes/ReceiptsListPage'
import { ReceiptDetailPage } from '../features/receipts/routes/ReceiptDetailPage'
import { IssuesListPage } from '../features/issues/routes/IssuesListPage'
import { IssueDetailPage } from '../features/issues/routes/IssueDetailPage'
import { TransfersListPage } from '../features/transfers/routes/TransfersListPage'
import { TransferDetailPage } from '../features/transfers/routes/TransferDetailPage'
import { AdjustmentsListPage } from '../features/adjustments/routes/AdjustmentsListPage'
import { AdjustmentDetailPage } from '../features/adjustments/routes/AdjustmentDetailPage'
import { WarehousesPage } from '../features/warehouses/routes/WarehousesPage'
import { StockItemsPage } from '../features/stock-items/routes/StockItemsPage'

const router = createBrowserRouter([
  {
    path: '/',
    element: <Layout />,
    errorElement: <RouteError />,
    children: [
      { index: true, element: <Navigate to="/receipts" replace /> },
      { path: 'receipts', element: <ReceiptsListPage /> },
      { path: 'receipts/:id', element: <ReceiptDetailPage /> },
      { path: 'issues', element: <IssuesListPage /> },
      { path: 'issues/:id', element: <IssueDetailPage /> },
      { path: 'transfers', element: <TransfersListPage /> },
      { path: 'transfers/:id', element: <TransferDetailPage /> },
      { path: 'adjustments', element: <AdjustmentsListPage /> },
      { path: 'adjustments/:id', element: <AdjustmentDetailPage /> },
      { path: 'warehouses', element: <WarehousesPage /> },
      { path: 'stock-items', element: <StockItemsPage /> },
    ],
  },
])

export function AppRouter() {
  return <RouterProvider router={router} />
}


