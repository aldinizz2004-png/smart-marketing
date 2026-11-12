import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/api/client'
import { unwrap } from '@/api/errors'
import type { components } from '@/api/schema'

export type SocialAccount = components['schemas']['SocialAccountResponse']
export type ConnectSocialAccountRequest = components['schemas']['ConnectSocialAccountRequest']

const key = (businessId: string) => ['social-accounts', businessId] as const

export function useSocialAccounts(businessId: string) {
  return useQuery({
    queryKey: key(businessId),
    queryFn: () => unwrap(api.GET('/api/businesses/{businessId}/social-accounts', { params: { path: { businessId } } })),
  })
}

export function useConnectSocialAccount(businessId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: ConnectSocialAccountRequest) =>
      unwrap(api.POST('/api/businesses/{businessId}/social-accounts', { params: { path: { businessId } }, body })),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: key(businessId) }),
  })
}

export function useConnectLink(businessId: string) {
  return useMutation({
    mutationFn: (platformCode: string) =>
      unwrap(api.POST('/api/businesses/{businessId}/social-accounts/connect-link', { params: { path: { businessId } }, body: { platformCode } })),
  })
}

export function useUpdateCredentials(businessId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, accessToken }: { id: string; accessToken: string }) =>
      unwrap(api.PUT('/api/businesses/{businessId}/social-accounts/{id}/credentials', { params: { path: { businessId, id } }, body: { accessToken } })),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: key(businessId) }),
  })
}

export function useDisconnectSocialAccount(businessId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) =>
      unwrap(api.POST('/api/businesses/{businessId}/social-accounts/{id}/disconnect', { params: { path: { businessId, id } } })),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: key(businessId) }),
  })
}
