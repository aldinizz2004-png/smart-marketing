import { createContext, useContext } from 'react'
import type { Business, Tenant } from './api'

export type Workspace = {
  tenant: Tenant
  businesses: Business[]
  business: Business
  businessId: string
  selectBusiness: (id: string) => void
}

export const WorkspaceContext = createContext<Workspace | null>(null)

// Pages inside the app shell always have a tenant with an active subscription and a current business.
export function useWorkspace(): Workspace {
  const workspace = useContext(WorkspaceContext)
  if (!workspace) throw new Error('useWorkspace is only available inside RequireWorkspace.')
  return workspace
}

export function useBusinessId(): string {
  return useWorkspace().businessId
}
