import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/api/client'
import { unwrap } from '@/api/errors'
import type { components } from '@/api/schema'

export type Campaign = components['schemas']['CampaignResponse']
export type CampaignRequest = components['schemas']['CampaignRequest']
export type CampaignObjective = components['schemas']['CampaignObjective']
export type CampaignPlanResponse = components['schemas']['CampaignPlanResponse']

export const objectives: CampaignObjective[] = ['Orders', 'Messages', 'Visits', 'Awareness', 'ProductPromotion', 'Followers']
export const campaignStatuses = ['Draft', 'Planning', 'Ready', 'Active', 'Paused', 'Completed', 'Canceled'] as const

// Same rules as CampaignService: Draft -> Ready -> Active -> Paused/Completed, and Canceled from anywhere open.
export const campaignNext: Record<string, string[]> = {
  Draft: ['Ready', 'Canceled'],
  Planning: ['Ready', 'Draft', 'Canceled'],
  Ready: ['Active', 'Draft', 'Canceled'],
  Active: ['Paused', 'Completed', 'Canceled'],
  Paused: ['Active', 'Completed', 'Canceled'],
  Completed: [],
  Canceled: [],
}

// What the AI returns as a campaign plan (CampaignPlanResponse.plan is untyped in the API).
export type CampaignPlan = {
  summary?: string
  objective?: string
  durationDays?: number
  channels?: { platform?: string; postsPerWeek?: number; bestTimes?: string[] }[]
  weeklyThemes?: string[]
  contentMix?: Record<string, number>
  kpis?: string[]
  budgetSplit?: { platform?: string; percent?: number; amount?: number }[]
  voice?: { dialect?: string | null; tone?: string | null; emojiLevel?: number | null }
  avoidTerms?: string[]
}

const keys = {
  all: (businessId: string) => ['campaigns', businessId] as const,
  item: (businessId: string, id: string) => ['campaigns', businessId, 'item', id] as const,
  plans: (businessId: string, id: string) => ['campaigns', businessId, 'plans', id] as const,
}

export function useCampaigns(businessId: string, { status, page = 1, pageSize = 50 }: { status?: string; page?: number; pageSize?: number } = {}) {
  return useQuery({
    queryKey: [...keys.all(businessId), 'list', status ?? '', page, pageSize],
    queryFn: () =>
      unwrap(api.GET('/api/businesses/{businessId}/campaigns', { params: { path: { businessId }, query: { Status: status || undefined, Page: page, PageSize: pageSize } } })),
    placeholderData: keepPreviousData,
  })
}

export function useCampaign(businessId: string, id: string) {
  return useQuery({
    queryKey: keys.item(businessId, id),
    queryFn: () => unwrap(api.GET('/api/businesses/{businessId}/campaigns/{id}', { params: { path: { businessId, id } } })),
  })
}

export function useSaveCampaign(businessId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, body }: { id?: string; body: CampaignRequest }) =>
      id
        ? unwrap(api.PUT('/api/businesses/{businessId}/campaigns/{id}', { params: { path: { businessId, id } }, body }))
        : unwrap(api.POST('/api/businesses/{businessId}/campaigns', { params: { path: { businessId } }, body })),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: keys.all(businessId) }),
  })
}

export function useChangeCampaignStatus(businessId: string, id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (status: string) =>
      unwrap(api.PATCH('/api/businesses/{businessId}/campaigns/{id}/status', { params: { path: { businessId, id } }, body: { status } })),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: keys.all(businessId) }),
  })
}

export function useCampaignPlans(businessId: string, id: string) {
  return useQuery({
    queryKey: keys.plans(businessId, id),
    queryFn: () => unwrap(api.GET('/api/businesses/{businessId}/campaigns/{id}/plans', { params: { path: { businessId, id } } })),
  })
}

export function useGeneratePlan(businessId: string, id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => unwrap(api.POST('/api/businesses/{businessId}/campaigns/{id}/plans', { params: { path: { businessId, id } } })),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: keys.all(businessId) }),
  })
}
