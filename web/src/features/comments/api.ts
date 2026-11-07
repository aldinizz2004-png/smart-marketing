import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/api/client'
import { unwrap } from '@/api/errors'
import type { components } from '@/api/schema'

export type Comment = components['schemas']['CommentResponse']
export type CommentCategory = components['schemas']['CommentCategory']

export const commentCategories: CommentCategory[] = ['Complaint', 'Question', 'PurchaseIntent', 'Praise', 'Spam', 'Other']
export const commentStatuses = ['New', 'Analyzed', 'Replied', 'Ignored'] as const

export type CommentFilters = { status?: string; category?: string; needsReply?: boolean; platform?: string; page?: number }

const key = (businessId: string) => ['comments', businessId] as const

function useInvalidate(businessId: string) {
  const queryClient = useQueryClient()
  return () => Promise.all([queryClient.invalidateQueries({ queryKey: key(businessId) }), queryClient.invalidateQueries({ queryKey: ['dashboard'] })])
}

export function useComments(businessId: string, filters: CommentFilters) {
  return useQuery({
    queryKey: [...key(businessId), filters],
    queryFn: () =>
      unwrap(
        api.GET('/api/businesses/{businessId}/comments', {
          params: {
            path: { businessId },
            query: {
              Status: filters.status || undefined,
              Category: (filters.category || undefined) as CommentCategory | undefined,
              NeedsReply: filters.needsReply || undefined,
              Platform: filters.platform || undefined,
              Page: filters.page ?? 1,
              PageSize: 20,
            },
          },
        }),
      ),
    placeholderData: keepPreviousData,
  })
}

export function useNewCommentsCount(businessId: string) {
  return useQuery({
    queryKey: [...key(businessId), 'new-count'],
    queryFn: async () =>
      (await unwrap(api.GET('/api/businesses/{businessId}/comments', { params: { path: { businessId }, query: { Status: 'New', PageSize: 1 } } })))
        .totalCount ?? 0,
  })
}

export function useSyncComments(businessId: string) {
  const invalidate = useInvalidate(businessId)
  return useMutation({
    mutationFn: () => unwrap(api.POST('/api/businesses/{businessId}/comments/sync', { params: { path: { businessId } } })),
    onSuccess: invalidate,
  })
}

export function useAnalyzeComments(businessId: string) {
  const invalidate = useInvalidate(businessId)
  return useMutation({
    mutationFn: () => unwrap(api.POST('/api/businesses/{businessId}/comments/analyze', { params: { path: { businessId } } })),
    onSuccess: invalidate,
  })
}

export function useReplyToComment(businessId: string) {
  const invalidate = useInvalidate(businessId)
  return useMutation({
    mutationFn: ({ id, text }: { id: string; text?: string | null }) =>
      unwrap(api.POST('/api/businesses/{businessId}/comments/{id}/reply', { params: { path: { businessId, id } }, body: { text: text || null } })),
    onSuccess: invalidate,
  })
}

export function useIgnoreComment(businessId: string) {
  const invalidate = useInvalidate(businessId)
  return useMutation({
    mutationFn: (id: string) => unwrap(api.POST('/api/businesses/{businessId}/comments/{id}/ignore', { params: { path: { businessId, id } } })),
    onSuccess: invalidate,
  })
}
