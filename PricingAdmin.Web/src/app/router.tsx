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
import { PermissionGate } from './permissions'
import { PricingPermissionKeys } from './pricingPermissionKeys'

const router = createBrowserRouter([
  {
    path: '/',
    element: <Layout />,
    errorElement: <RouteError />,
    children: [
      { index: true, element: <Navigate to="/pricing/home" replace /> },
      { path: 'pricing/home', element: <PricingHomePage /> },
      {
        path: 'pricing/policies',
        element: (
          <PermissionGate anyPermissions={[PricingPermissionKeys.PoliciesListView, PricingPermissionKeys.PoliciesView, PricingPermissionKeys.PoliciesEdit]}>
            <PricingPolicyPage />
          </PermissionGate>
        ),
      },
      {
        path: 'pricing/pricelists',
        element: (
          <PermissionGate permission={PricingPermissionKeys.PriceListsView}>
            <PriceListsPage />
          </PermissionGate>
        ),
      },
      {
        path: 'pricing/pricelists/:id',
        element: (
          <PermissionGate permission={PricingPermissionKeys.PriceListsDetailView}>
            <PriceListDetailPage />
          </PermissionGate>
        ),
      },
      {
        path: 'pricing/overrides',
        element: (
          <PermissionGate permission={PricingPermissionKeys.OverridesView}>
            <PriceOverridesPage />
          </PermissionGate>
        ),
      },
      {
        path: 'pricing/campaigns',
        element: (
          <PermissionGate permission={PricingPermissionKeys.CampaignsView}>
            <CampaignsPage />
          </PermissionGate>
        ),
      },
      {
        path: 'pricing/coupons',
        element: (
          <PermissionGate permission={PricingPermissionKeys.CouponsView}>
            <CouponsPage />
          </PermissionGate>
        ),
      },
      {
        path: 'pricing/bulk',
        element: (
          <PermissionGate anyPermissions={[PricingPermissionKeys.BulkPricesUpdate, PricingPermissionKeys.BulkCampaignsCategoryCreate]}>
            <BulkOperationsPage />
          </PermissionGate>
        ),
      },
      {
        path: 'pricing/quote',
        element: (
          <PermissionGate permission={PricingPermissionKeys.QuotesCreate}>
            <QuotePreviewPage />
          </PermissionGate>
        ),
      },
      {
        path: 'pricing/product',
        element: (
          <PermissionGate anyPermissions={[
            PricingPermissionKeys.PriceListsView,
            PricingPermissionKeys.PriceListsEdit,
            PricingPermissionKeys.OverridesView,
            PricingPermissionKeys.OverridesCreate,
            PricingPermissionKeys.CampaignsView,
            PricingPermissionKeys.CampaignsCreate,
          ]}>
            <ProductPricingPage />
          </PermissionGate>
        ),
      },
      {
        path: 'pricing/report',
        element: (
          <PermissionGate anyPermissions={[PricingPermissionKeys.PriceListsView, PricingPermissionKeys.OverridesView, PricingPermissionKeys.CampaignsView]}>
            <PricingReportPage />
          </PermissionGate>
        ),
      },
    ],
  },
])

export function AppRouter() {
  return <RouterProvider router={router} />
}
