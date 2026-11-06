import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router'
import '@/i18n'
import '@/styles/index.css'
import { Providers } from '@/app/providers'
import { AppRoutes } from '@/app/routes'
import { restoreSession } from '@/auth/auth-api'

// A returning visitor still has the refresh cookie: turn it into a session before the first page decides where to go.
void restoreSession()

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <Providers>
      <BrowserRouter>
        <AppRoutes />
      </BrowserRouter>
    </Providers>
  </StrictMode>,
)
