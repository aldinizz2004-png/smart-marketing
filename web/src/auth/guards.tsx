import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router'
import { FullPageSpinner } from '@/components/ui/spinner'
import { useSessionState } from './use-session'

export function RequireAuth({ children }: { children: ReactNode }) {
  const state = useSessionState()
  const location = useLocation()

  if (state.status === 'loading') return <FullPageSpinner />
  if (state.status === 'anonymous') return <Navigate to="/login" replace state={{ from: location.pathname + location.search }} />
  return children
}

export function PublicOnly({ children }: { children: ReactNode }) {
  const state = useSessionState()

  if (state.status === 'loading') return <FullPageSpinner />
  if (state.status === 'authenticated') return <Navigate to="/" replace />
  return children
}
