import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/api/client'
import { unwrap } from '@/api/errors'
import type { components } from '@/api/schema'
import { workspaceKeys } from '@/features/workspace/api'

export type AutomationPolicy = components['schemas']['AutomationPolicyResponse']
export type AutomationPolicyRequest = components['schemas']['AutomationPolicyRequest']
export type RecommendationAction = components['schemas']['RecommendationAction']
export type Member = components['schemas']['MemberResponse']
export type MembershipRole = components['schemas']['MembershipRole']
export type AiUsage = components['schemas']['AiUsageResponse']

export const recommendationActions: RecommendationAction[] = ['NewContent', 'ReviseCopy', 'ChangeFormat', 'ChangeChannel', 'Reschedule', 'Pause']
export const assignableRoles: MembershipRole[] = ['Admin', 'Editor', 'Reviewer', 'Viewer']

const keys = {
  policy: (businessId: string) => ['automation-policy', businessId] as const,
  members: ['members'] as const,
  usage: (days: number) => ['ai-usage', days] as const,
}

export function useAutomationPolicy(businessId: string) {
  return useQuery({
    queryKey: keys.policy(businessId),
    queryFn: () => unwrap(api.GET('/api/businesses/{businessId}/automation-policy', { params: { path: { businessId } } })),
  })
}

export function useSaveAutomationPolicy(businessId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: AutomationPolicyRequest) =>
      unwrap(api.PUT('/api/businesses/{businessId}/automation-policy', { params: { path: { businessId } }, body })),
    onSuccess: (policy) => queryClient.setQueryData(keys.policy(businessId), policy),
  })
}

export function useMembers() {
  return useQuery({
    queryKey: keys.members,
    queryFn: () => unwrap(api.GET('/api/tenant/members', { params: { query: { PageSize: 100 } } })),
  })
}

export function useAddMember() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: { email: string; role: MembershipRole }) => unwrap(api.POST('/api/tenant/members', { body })),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: keys.members }),
  })
}

export function useUpdateMember() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ userId, ...body }: { userId: string; role?: MembershipRole; isActive?: boolean }) =>
      unwrap(api.PATCH('/api/tenant/members/{userId}', { params: { path: { userId } }, body })),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: keys.members }),
  })
}

export function useRenameTenant() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (name: string) => unwrap(api.PATCH('/api/tenant', { body: { name } })),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: workspaceKeys.tenant }),
  })
}

export function useAiUsage(days: number) {
  return useQuery({
    queryKey: keys.usage(days),
    queryFn: () => {
      const to = new Date()
      const from = new Date(to.getTime() - days * 86_400_000)
      return unwrap(api.GET('/api/tenant/ai-usage', { params: { query: { From: from.toISOString(), To: to.toISOString() } } }))
    },
  })
}
