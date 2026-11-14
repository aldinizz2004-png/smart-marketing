import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/api/client'
import { unwrap, unwrapEmpty } from '@/api/errors'
import type { components } from '@/api/schema'

export type TrackedSource = components['schemas']['TrackedSourceResponse']
export type TrackedSourceKind = components['schemas']['TrackedSourceKind']
export type TrackedSourceRequest = components['schemas']['TrackedSourceRequest']
export type SourcePost = components['schemas']['SourcePostResponse']
export type CompetitorInsight = components['schemas']['CompetitorInsightResponse']
export type InsightType = components['schemas']['InsightType']

export const sourceKinds: TrackedSourceKind[] = ['Competitor', 'Professional', 'Inspiration']

const keys = {
  sources: (businessId: string) => ['tracked-sources', businessId] as const,
  posts: (businessId: string, id: string) => ['tracked-source-posts', businessId, id] as const,
  insights: (businessId: string) => ['competitor-insights', businessId] as const,
}

export function useTrackedSources(businessId: string, kind?: string) {
  return useQuery({
    queryKey: [...keys.sources(businessId), kind ?? ''],
    queryFn: () =>
      unwrap(
        api.GET('/api/businesses/{businessId}/tracked-sources', {
          params: { path: { businessId }, query: { Kind: (kind || undefined) as TrackedSourceKind | undefined, PageSize: 50 } },
        }),
      ),
  })
}

export function useAddTrackedSource(businessId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: TrackedSourceRequest) => unwrap(api.POST('/api/businesses/{businessId}/tracked-sources', { params: { path: { businessId } }, body })),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: keys.sources(businessId) }),
  })
}

export function useChangeSourceStatus(businessId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, status }: { id: string; status: string }) =>
      unwrap(api.PATCH('/api/businesses/{businessId}/tracked-sources/{id}/status', { params: { path: { businessId, id } }, body: { status } })),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: keys.sources(businessId) }),
  })
}

export function useScanSource(businessId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => unwrap(api.POST('/api/businesses/{businessId}/tracked-sources/{id}/scan', { params: { path: { businessId, id } } })),
    onSuccess: (_, id) =>
      Promise.all([queryClient.invalidateQueries({ queryKey: keys.sources(businessId) }), queryClient.invalidateQueries({ queryKey: keys.posts(businessId, id) })]),
  })
}

export function useDeleteSource(businessId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => unwrapEmpty(api.DELETE('/api/businesses/{businessId}/tracked-sources/{id}', { params: { path: { businessId, id } } })),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: keys.sources(businessId) }),
  })
}

export function useSourcePosts(businessId: string, id: string | undefined, page: number) {
  return useQuery({
    queryKey: [...keys.posts(businessId, id ?? ''), page],
    enabled: !!id,
    placeholderData: keepPreviousData,
    queryFn: () =>
      unwrap(api.GET('/api/businesses/{businessId}/tracked-sources/{id}/posts', { params: { path: { businessId, id: id! }, query: { Page: page, PageSize: 10 } } })),
  })
}

export function useCompetitorInsights(businessId: string) {
  return useQuery({
    queryKey: keys.insights(businessId),
    queryFn: () => unwrap(api.GET('/api/businesses/{businessId}/competitor-insights', { params: { path: { businessId } } })),
  })
}

export function useAnalyzeInsights(businessId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => unwrap(api.POST('/api/businesses/{businessId}/competitor-insights/analyze', { params: { path: { businessId } } })),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: keys.insights(businessId) }),
  })
}
