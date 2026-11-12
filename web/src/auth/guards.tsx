import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router'
import { FullPageSpinner } from '@/components/ui/spinner'
import { maybeSignedIn } from './auth-api'
import { useSessionState } from './use-session'

export function RequireAuth({ children }: { children: ReactNode }) {
  const state = useSessionState()
  const location = useLocation()

  if (state.status === 'loading') return <FullPageSpinner />
  if (state.status === 'anonymous') {
    // A first-time visitor at the root sees what the product is before being asked to sign in.
    if (location.pathname === '/' && !maybeSignedIn()) return <Navigate to="/welcome" replace />
    return <Navigate to="/login" replace state={{ from: location.pathname + location.search }} />
  }
  return children
}

export function PublicOnly({ children }: { children: ReactNode }) {
  const state = useSessionState()

  if (state.status === 'loading') return <FullPageSpinner />
  if (state.status === 'authenticated') return <Navigate to="/" replace />
  return children
}
