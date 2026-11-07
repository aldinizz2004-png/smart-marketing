import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/api/client'
import { unwrap } from '@/api/errors'
import type { components } from '@/api/schema'

export type ContentItem = components['schemas']['ContentItemResponse']
export type Revision = components['schemas']['ContentRevisionResponse']
export type RevisionRequest = components['schemas']['RevisionRequest']
export type CreateContentItemRequest = components['schemas']['CreateContentItemRequest']
export type PublicationJob = components['schemas']['PublicationJobResponse']
export type ContentType = components['schemas']['ContentType']
export type ContentFormat = components['schemas']['ContentFormat']
export type HookType = components['schemas']['HookType']
export type CallToAction = components['schemas']['CallToAction']

export const contentTypes: ContentType[] = ['Post', 'Story', 'Reel', 'Short', 'AdCopy', 'WhatsAppMessage']
export const contentFormats: ContentFormat[] = ['Text', 'SingleImage', 'Carousel', 'Video', 'Reel', 'Story', 'Live']
export const hookTypes: HookType[] = ['Question', 'Offer', 'Story', 'SocialProof', 'Urgency', 'Informative', 'Humor', 'Tutorial']
export const callsToAction: CallToAction[] = ['OrderNow', 'SendMessage', 'VisitStore', 'LearnMore', 'Follow', 'None']
export const contentStatuses = ['Draft', 'InReview', 'Approved', 'Scheduled', 'Published', 'Canceled'] as const

export type ContentFilters = { status?: string; platform?: string; campaignId?: string; page?: number }

export const contentKeys = {
  all: (businessId: string) => ['content', businessId] as const,
  list: (businessId: string, filters: ContentFilters) => ['content', businessId, 'list', filters] as const,
  item: (businessId: string, id: string) => ['content', businessId, 'item', id] as const,
  jobs: (businessId: string, status?: string) => ['content', businessId, 'jobs', status ?? 'all'] as const,
}

export function useContentItems(businessId: string, filters: ContentFilters) {
  return useQuery({
    queryKey: contentKeys.list(businessId, filters),
    queryFn: () =>
      unwrap(
        api.GET('/api/businesses/{businessId}/content-items', {
          params: {
            path: { businessId },
            query: {
              Status: filters.status || undefined,
              Platform: filters.platform || undefined,
              CampaignId: filters.campaignId || undefined,
              Page: filters.page ?? 1,
              PageSize: 12,
            },
          },
        }),
      ),
    placeholderData: keepPreviousData,
  })
}

export function useContentItem(businessId: string, id: string) {
  return useQuery({
    queryKey: contentKeys.item(businessId, id),
    queryFn: () => unwrap(api.GET('/api/businesses/{businessId}/content-items/{id}', { params: { path: { businessId, id } } })),
  })
}

function useInvalidateContent(businessId: string) {
  const queryClient = useQueryClient()
  return () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: contentKeys.all(businessId) }),
      queryClient.invalidateQueries({ queryKey: ['dashboard'] }),
    ])
}

export function useCreateContentItem(businessId: string) {
  const invalidate = useInvalidateContent(businessId)
  return useMutation({
    mutationFn: (body: CreateContentItemRequest) =>
      unwrap(api.POST('/api/businesses/{businessId}/content-items', { params: { path: { businessId } }, body })),
    onSuccess: invalidate,
  })
}

export function useAddRevision(businessId: string, id: string) {
  const invalidate = useInvalidateContent(businessId)
  return useMutation({
    mutationFn: (body: RevisionRequest) =>
      unwrap(api.POST('/api/businesses/{businessId}/content-items/{id}/revisions', { params: { path: { businessId, id } }, body })),
    onSuccess: invalidate,
  })
}

export function useChangeContentStatus(businessId: string, id: string) {
  const invalidate = useInvalidateContent(businessId)
  return useMutation({
    mutationFn: (status: string) =>
      unwrap(api.PATCH('/api/businesses/{businessId}/content-items/{id}/status', { params: { path: { businessId, id } }, body: { status } })),
    onSuccess: invalidate,
  })
}

export function useReviewContent(businessId: string, id: string) {
  const invalidate = useInvalidateContent(businessId)
  return useMutation({
    mutationFn: (body: { decision: 'Approved' | 'ChangesRequested' | 'Rejected'; feedback?: string | null }) =>
      unwrap(api.POST('/api/businesses/{businessId}/content-items/{id}/reviews', { params: { path: { businessId, id } }, body })),
    onSuccess: invalidate,
  })
}

export function useScheduleContent(businessId: string, id: string) {
  const invalidate = useInvalidateContent(businessId)
  return useMutation({
    mutationFn: (body: { socialAccountId?: string | null; publishAt?: string | null }) =>
      unwrap(api.POST('/api/businesses/{businessId}/content-items/{id}/schedule', { params: { path: { businessId, id } }, body })),
    onSuccess: invalidate,
  })
}

export function usePublicationJobs(businessId: string, status?: string) {
  return useQuery({
    queryKey: contentKeys.jobs(businessId, status),
    queryFn: () =>
      unwrap(
        api.GET('/api/businesses/{businessId}/publication-jobs', {
          params: { path: { businessId }, query: { Status: status || undefined, PageSize: 100 } },
        }),
      ),
    refetchInterval: (query) => ((query.state.data?.items ?? []).some((j) => ['Queued', 'Running', 'Retry'].includes(j.status?.code ?? '')) ? 5000 : false),
  })
}

export function useCancelJob(businessId: string) {
  const invalidate = useInvalidateContent(businessId)
  return useMutation({
    mutationFn: (jobId: string) =>
      unwrap(api.POST('/api/businesses/{businessId}/publication-jobs/{jobId}/cancel', { params: { path: { businessId, jobId } } })),
    onSuccess: invalidate,
  })
}

export function usePublicationMetrics(businessId: string, publicationId: string | null | undefined) {
  return useQuery({
    queryKey: ['metrics', businessId, publicationId],
    queryFn: () =>
      unwrap(
        api.GET('/api/businesses/{businessId}/publications/{publicationId}/metrics', {
          params: { path: { businessId, publicationId: publicationId ?? '' } },
        }),
      ),
    enabled: Boolean(publicationId),
  })
}

export function useRefreshMetrics(businessId: string, publicationId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () =>
      unwrap(
        api.POST('/api/businesses/{businessId}/publications/{publicationId}/metrics/refresh', {
          params: { path: { businessId, publicationId } },
        }),
      ),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['metrics', businessId, publicationId] }),
  })
}
