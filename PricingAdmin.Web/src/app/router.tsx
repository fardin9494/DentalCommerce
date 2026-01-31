import { createBrowserRouter, RouterProvider, Navigate } from 'react-router-dom'
import { Layout } from '../shared/components/Layout'
import { RouteError } from './RouteError'
import { PricingPolicyPage } from '../features/pricing/routes/PricingPolicyPage'
import { PriceListsPage } from '../features/pricing/routes/PriceListsPage'
import { PriceListDetailPage } from '../features/pricing/routes/PriceListDetailPage'
import { QuotePreviewPage } from '../features/pricing/routes/QuotePreviewPage'
import { PriceOverridesPage } from '../features/pricing/routes/PriceOverridesPage'
import { CampaignsPage } from '../features/pricing/routes/CampaignsPage'
import { CouponsPage } from '../features/pricing/routes/CouponsPage'
import { BulkOperationsPage } from '../features/pricing/routes/BulkOperationsPage'
import { ProductPricingPage } from '../features/pricing/routes/ProductPricingPage'
import { PricingHomePage } from '../features/pricing/routes/PricingHomePage'
import { PricingReportPage } from '../features/pricing/routes/PricingReportPage'

const router = createBrowserRouter([
  {
    path: '/',
    element: <Layout />,
    errorElement: <RouteError />,
    children: [
      { index: true, element: <Navigate to="/pricing/home" replace /> },
      { path: 'pricing/home', element: <PricingHomePage /> },
      { path: 'pricing/policies', element: <PricingPolicyPage /> },
      { path: 'pricing/pricelists', element: <PriceListsPage /> },
      { path: 'pricing/pricelists/:id', element: <PriceListDetailPage /> },
      { path: 'pricing/overrides', element: <PriceOverridesPage /> },
      { path: 'pricing/campaigns', element: <CampaignsPage /> },
      { path: 'pricing/coupons', element: <CouponsPage /> },
      { path: 'pricing/bulk', element: <BulkOperationsPage /> },
      { path: 'pricing/quote', element: <QuotePreviewPage /> },
      { path: 'pricing/product', element: <ProductPricingPage /> },
      { path: 'pricing/report', element: <PricingReportPage /> },
    ],
  },
])

export function AppRouter() {
  return <RouterProvider router={router} />
}
