import { useCallback, useMemo, useState, type ReactNode } from 'react'
import { Navigate } from 'react-router'
import { ErrorState } from '@/components/error-state'
import { FullPageSpinner } from '@/components/ui/spinner'
import { useBusinesses, useTenant } from './api'
import { WorkspaceContext, type Workspace } from './workspace-context'

const storageKey = (tenantId: string) => `smp.business.${tenantId}`

function readStored(tenantId: string) {
  try {
    return localStorage.getItem(storageKey(tenantId))
  } catch {
    return null
  }
}

// Sends the user to onboarding until there is an active subscription and at least one open business.
export function RequireWorkspace({ children }: { children: ReactNode }) {
  const tenant = useTenant()
  const hasAccess = tenant.data?.currentSubscription?.grantsAccessNow === true
  const businesses = useBusinesses(hasAccess)
  const [selectedId, setSelectedId] = useState<string | null>(null)

  const openBusinesses = useMemo(
    () => (businesses.data?.items ?? []).filter((b) => b.status?.code !== 'Closed'),
    [businesses.data],
  )

  const tenantId = tenant.data?.id
  const selectBusiness = useCallback(
    (id: string) => {
      setSelectedId(id)
      try {
        if (tenantId) localStorage.setItem(storageKey(tenantId), id)
      } catch {
        // storage blocked: the choice lasts for this visit
      }
    },
    [tenantId],
  )

  const workspace = useMemo<Workspace | null>(() => {
    if (!tenant.data || openBusinesses.length === 0) return null
    const wanted = selectedId ?? readStored(tenant.data.id ?? '')
    const business = openBusinesses.find((b) => b.id === wanted) ?? openBusinesses[0]!
    return { tenant: tenant.data, businesses: openBusinesses, business, businessId: business.id ?? '', selectBusiness }
  }, [tenant.data, openBusinesses, selectedId, selectBusiness])

  if (tenant.isPending || (hasAccess && businesses.isPending)) return <FullPageSpinner />
  if (tenant.isError) return <CenteredError error={tenant.error} onRetry={() => void tenant.refetch()} />
  if (businesses.isError) return <CenteredError error={businesses.error} onRetry={() => void businesses.refetch()} />
  if (!hasAccess || !workspace) return <Navigate to="/onboarding" replace />

  return <WorkspaceContext.Provider value={workspace}>{children}</WorkspaceContext.Provider>
}

function CenteredError({ error, onRetry }: { error: unknown; onRetry: () => void }) {
  return (
    <div className="mx-auto grid min-h-dvh max-w-lg place-items-center p-6">
      <ErrorState error={error} onRetry={onRetry} />
    </div>
  )
}
