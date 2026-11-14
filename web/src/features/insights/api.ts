import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/api/client'
import { unwrap } from '@/api/errors'
import type { components } from '@/api/schema'

export type Performance = components['schemas']['PerformanceResponse']
export type PerformanceGroup = components['schemas']['PerformanceGroup']
export type Recommendation = components['schemas']['RecommendationResponse']
export type RecommendationAction = components['schemas']['RecommendationAction']
export type StrategyRun = components['schemas']['StrategyRunResponse']

export const rangeDays = [7, 30, 90] as const
export const recommendationStatuses = ['Pending', 'Accepted', 'Rejected', 'Applied'] as const
export const runWindows = [7, 14, 30] as const

const keys = {
  performance: (businessId: string) => ['performance', businessId] as const,
  recommendations: (businessId: string) => ['recommendations', businessId] as const,
  runs: (businessId: string) => ['strategy-runs', businessId] as const,
}

export function usePerformance(businessId: string, days: number) {
  return useQuery({
    queryKey: [...keys.performance(businessId), days],
    queryFn: () => {
      const to = new Date()
      const from = new Date(to.getTime() - days * 86_400_000)
      return unwrap(api.GET('/api/businesses/{businessId}/performance', { params: { path: { businessId }, query: { From: from.toISOString(), To: to.toISOString() } } }))
    },
    placeholderData: keepPreviousData,
  })
}

export function useRecommendations(businessId: string, { status, page = 1 }: { status?: string; page?: number }) {
  return useQuery({
    queryKey: [...keys.recommendations(businessId), status ?? '', page],
    queryFn: () =>
      unwrap(api.GET('/api/businesses/{businessId}/recommendations', { params: { path: { businessId }, query: { Status: status || undefined, Page: page, PageSize: 20 } } })),
    placeholderData: keepPreviousData,
  })
}

function useRefresh(businessId: string) {
  const queryClient = useQueryClient()
  return () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: keys.recommendations(businessId) }),
      queryClient.invalidateQueries({ queryKey: keys.runs(businessId) }),
      queryClient.invalidateQueries({ queryKey: ['content', businessId] }),
      queryClient.invalidateQueries({ queryKey: ['dashboard'] }),
    ])
}

export function useDecideRecommendation(businessId: string) {
  const refresh = useRefresh(businessId)
  return useMutation({
    mutationFn: ({ id, status }: { id: string; status: 'Accepted' | 'Rejected' }) =>
      unwrap(api.PATCH('/api/businesses/{businessId}/recommendations/{id}/status', { params: { path: { businessId, id } }, body: { status } })),
    onSuccess: refresh,
  })
}

export function useApplyRecommendation(businessId: string) {
  const refresh = useRefresh(businessId)
  return useMutation({
    mutationFn: ({ id, campaignId }: { id: string; campaignId?: string }) =>
      unwrap(api.POST('/api/businesses/{businessId}/recommendations/{id}/apply', { params: { path: { businessId, id } }, body: { campaignId: campaignId ?? null } })),
    onSuccess: refresh,
  })
}

export function useStrategyRuns(businessId: string, page: number) {
  return useQuery({
    queryKey: [...keys.runs(businessId), page],
    queryFn: () => unwrap(api.GET('/api/businesses/{businessId}/strategy-runs', { params: { path: { businessId }, query: { Page: page, PageSize: 10 } } })),
    placeholderData: keepPreviousData,
  })
}

export function useStartStrategyRun(businessId: string) {
  const refresh = useRefresh(businessId)
  return useMutation({
    mutationFn: ({ windowDays, campaignId }: { windowDays: number; campaignId?: string }) =>
      unwrap(api.POST('/api/businesses/{businessId}/strategy-runs', { params: { path: { businessId } }, body: { windowDays, campaignId: campaignId || null } })),
    onSuccess: refresh,
  })
}

// The proposed payload is free-form JSON written by the AI; read only the keys the page shows.
export function readPayload(payload: unknown) {
  const source = typeof payload === 'object' && payload !== null ? (payload as Record<string, unknown>) : {}
  const text = (key: string) => (typeof source[key] === 'string' && source[key] ? (source[key] as string) : null)
  return { platform: text('platform'), format: text('format'), hook: text('hook'), captionIdea: text('captionIdea'), contentType: text('contentType') }
}
