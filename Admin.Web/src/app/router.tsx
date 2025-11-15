import { createBrowserRouter, RouterProvider, Navigate } from 'react-router-dom'
import { Layout } from '../shared/components/Layout'
import { RouteError } from './RouteError'
import { ProductsListPage } from '../features/products/routes/ProductsListPage'
import { ProductDetailPage } from '../features/products/routes/ProductDetailPage'
import { CreateProductPage } from '../features/products/routes/CreateProductPage'
import { BrandsPage } from '../features/brands/routes/BrandsPage'
import { BrandDetailPage } from '../features/brands/routes/BrandDetailPage'
import { CountriesPage } from '../features/countries/routes/CountriesPage'
import { StoresPage } from '../features/stores/routes/StoresPage'
import { CategoriesPage } from '../features/categories/routes/CategoriesPage'

const router = createBrowserRouter([
  {
    path: '/',
    element: <Layout />,
    errorElement: <RouteError />,
    children: [
      { index: true, element: <Navigate to="/products" replace /> },
      { path: 'products', element: <ProductsListPage /> },
      { path: 'products/new', element: <CreateProductPage /> },
      { path: 'products/:id', element: <ProductDetailPage /> },
      { path: 'brands', element: <BrandsPage /> },
      { path: 'brands/:id', element: <BrandDetailPage /> },
      { path: 'stores', element: <StoresPage /> },
      { path: 'countries', element: <CountriesPage /> },
      { path: 'categories', element: <CategoriesPage /> },
    ],
  },
])

export function AppRouter() {
  return <RouterProvider router={router} />
}
