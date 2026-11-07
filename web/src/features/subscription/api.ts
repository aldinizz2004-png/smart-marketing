import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/api/client'
import { unwrap } from '@/api/errors'
import type { components } from '@/api/schema'
import { workspaceKeys } from '@/features/workspace/api'

export type Plan = components['schemas']['PlanResponse']
export type PlanName = components['schemas']['SubscriptionPlan']
export type Duration = components['schemas']['SubscriptionDuration']
export type Subscription = components['schemas']['SubscriptionResponse']

export const durations: Duration[] = ['OneMonth', 'ThreeMonths', 'OneYear']

const subscriptionsKey = ['subscriptions'] as const

export function usePlans() {
  return useQuery({ queryKey: ['plans'], queryFn: () => unwrap(api.GET('/api/plans')), staleTime: 60 * 60_000 })
}

export function useSubscriptions() {
  return useQuery({
    queryKey: subscriptionsKey,
    queryFn: () => unwrap(api.GET('/api/tenant/subscriptions', { params: { query: { PageSize: 50 } } })),
  })
}

function useInvalidateBilling() {
  const queryClient = useQueryClient()
  return () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: subscriptionsKey }),
      queryClient.invalidateQueries({ queryKey: workspaceKeys.tenant }),
    ])
}

export function useCreateSubscription() {
  const invalidate = useInvalidateBilling()
  return useMutation({
    mutationFn: (body: { plan: PlanName; duration: Duration }) => unwrap(api.POST('/api/tenant/subscriptions', { body })),
    onSuccess: invalidate,
  })
}

export function useActivateSubscription() {
  const invalidate = useInvalidateBilling()
  return useMutation({
    mutationFn: ({ id, paymentReference }: { id: string; paymentReference: string }) =>
      unwrap(api.POST('/api/tenant/subscriptions/{id}/activate', { params: { path: { id } }, body: { paymentReference } })),
    onSuccess: invalidate,
  })
}

export function useCancelSubscription() {
  const invalidate = useInvalidateBilling()
  return useMutation({
    mutationFn: (id: string) => unwrap(api.POST('/api/tenant/subscriptions/{id}/cancel', { params: { path: { id } } })),
    onSuccess: invalidate,
  })
}
