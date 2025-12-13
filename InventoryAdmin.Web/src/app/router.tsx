import { createBrowserRouter, RouterProvider, Navigate } from 'react-router-dom'
import { Layout } from '../shared/components/Layout'
import { RouteError } from './RouteError'
import { ReceiptsListPage } from '../features/receipts/routes/ReceiptsListPage'
import { ReceiptDetailPage } from '../features/receipts/routes/ReceiptDetailPage'
import { IssuesListPage } from '../features/issues/routes/IssuesListPage'
import { IssueDetailPage } from '../features/issues/routes/IssueDetailPage'
import { PickingPlanPage } from '../features/issues/routes/PickingPlanPage'
import { TransfersListPage } from '../features/transfers/routes/TransfersListPage'
import { TransferDetailPage } from '../features/transfers/routes/TransferDetailPage'
import { AdjustmentsListPage } from '../features/adjustments/routes/AdjustmentsListPage'
import { AdjustmentDetailPage } from '../features/adjustments/routes/AdjustmentDetailPage'
import { WarehousesPage } from '../features/warehouses/routes/WarehousesPage'
import { StockItemsPage } from '../features/stock-items/routes/StockItemsPage'
import { ShelvesPage } from '../features/shelves/routes/ShelvesPage'
import { PutAwayPage } from '../features/put-away/routes/PutAwayPage'
import { ShelfTransferPage } from '../features/shelf-transfer/routes/ShelfTransferPage'
import { StockLedgerPage } from '../features/stock-ledger/routes/StockLedgerPage'

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
      { path: 'issues/picking-plan', element: <PickingPlanPage /> },
      { path: 'transfers', element: <TransfersListPage /> },
      { path: 'transfers/:id', element: <TransferDetailPage /> },
      { path: 'adjustments', element: <AdjustmentsListPage /> },
      { path: 'adjustments/:id', element: <AdjustmentDetailPage /> },
      { path: 'warehouses', element: <WarehousesPage /> },
      { path: 'stock-items', element: <StockItemsPage /> },
      { path: 'shelves', element: <ShelvesPage /> },
      { path: 'put-away', element: <PutAwayPage /> },
      { path: 'shelf-transfer', element: <ShelfTransferPage /> },
      { path: 'stock-ledger', element: <StockLedgerPage /> },
    ],
  },
])

export function AppRouter() {
  return <RouterProvider router={router} />
}


