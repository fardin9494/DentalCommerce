import React from 'react'
import { createRoot } from 'react-dom/client'
import { AppProviders } from './app/providers'
import { AppRouter } from './app/router'
import './index.css'
import './shared/utils/alert-polyfill'

const container = document.getElementById('root')!
const root = createRoot(container)
root.render(
  <React.StrictMode>
    <AppProviders>
      <AppRouter />
    </AppProviders>
  </React.StrictMode>
)


