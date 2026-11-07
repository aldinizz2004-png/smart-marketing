import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/api/client'
import { unwrap } from '@/api/errors'
import type { components } from '@/api/schema'

export type Tenant = components['schemas']['TenantResponse']
export type Business = components['schemas']['BusinessResponse']
export type BusinessRequest = components['schemas']['BusinessRequest']

export const workspaceKeys = {
  tenant: ['tenant'] as const,
  businesses: ['businesses'] as const,
}

export function useTenant() {
  return useQuery({ queryKey: workspaceKeys.tenant, queryFn: () => unwrap(api.GET('/api/tenant')) })
}

export function useBusinesses(enabled: boolean) {
  return useQuery({
    queryKey: workspaceKeys.businesses,
    queryFn: () => unwrap(api.GET('/api/businesses', { params: { query: { PageSize: 100 } } })),
    enabled,
  })
}

export function useCreateBusiness() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: BusinessRequest) => unwrap(api.POST('/api/businesses', { body })),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: workspaceKeys.businesses }),
  })
}

export function useUpdateBusiness(id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: BusinessRequest) => unwrap(api.PUT('/api/businesses/{id}', { params: { path: { id } }, body })),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: workspaceKeys.businesses }),
  })
}

export function useChangeBusinessStatus(id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (status: string) =>
      unwrap(api.PATCH('/api/businesses/{id}/status', { params: { path: { id } }, body: { status } })),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: workspaceKeys.businesses }),
  })
}
