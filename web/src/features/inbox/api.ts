import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/api/client'
import { unwrap } from '@/api/errors'
import type { components } from '@/api/schema'

export type ConversationSummary = components['schemas']['ConversationSummaryResponse']
export type Conversation = components['schemas']['ConversationResponse']
export type Message = components['schemas']['MessageResponse']
export type ConversationIntent = components['schemas']['ConversationIntent']
export type SendMessageRequest = components['schemas']['SendMessageRequest']

export const conversationStatuses = ['Open', 'WaitingForOwner', 'Closed'] as const
export const conversationIntents: ConversationIntent[] = ['Order', 'Question', 'Complaint', 'Greeting', 'Other']

export type ConversationFilters = { status?: string; intent?: string; page?: number }

const key = (businessId: string) => ['conversations', businessId] as const

export function useConversations(businessId: string, filters: ConversationFilters) {
  return useQuery({
    queryKey: [...key(businessId), 'list', filters],
    queryFn: () =>
      unwrap(
        api.GET('/api/businesses/{businessId}/conversations', {
          params: {
            path: { businessId },
            query: {
              Status: filters.status || undefined,
              Intent: (filters.intent || undefined) as ConversationIntent | undefined,
              Page: filters.page ?? 1,
              PageSize: 20,
            },
          },
        }),
      ),
    placeholderData: keepPreviousData,
    refetchInterval: 30_000,
  })
}

export function useConversation(businessId: string, id: string) {
  return useQuery({
    queryKey: [...key(businessId), 'detail', id],
    queryFn: () => unwrap(api.GET('/api/businesses/{businessId}/conversations/{id}', { params: { path: { businessId, id } } })),
    enabled: id !== '',
    refetchInterval: 15_000,
  })
}

function useUpdate(businessId: string) {
  const queryClient = useQueryClient()
  return (data: Conversation) => {
    if (data.conversation?.id) queryClient.setQueryData([...key(businessId), 'detail', data.conversation.id], data)
    return Promise.all([
      queryClient.invalidateQueries({ queryKey: [...key(businessId), 'list'] }),
      queryClient.invalidateQueries({ queryKey: ['dashboard'] }),
    ])
  }
}

export function useSendMessage(businessId: string, id: string) {
  const update = useUpdate(businessId)
  return useMutation({
    mutationFn: (body: SendMessageRequest) =>
      unwrap(api.POST('/api/businesses/{businessId}/conversations/{id}/messages', { params: { path: { businessId, id } }, body })),
    onSuccess: update,
  })
}

export function useSuggestReply(businessId: string, id: string) {
  const update = useUpdate(businessId)
  return useMutation({
    mutationFn: () => unwrap(api.POST('/api/businesses/{businessId}/conversations/{id}/suggest', { params: { path: { businessId, id } } })),
    onSuccess: update,
  })
}

export function useChangeConversationStatus(businessId: string, id: string) {
  const update = useUpdate(businessId)
  return useMutation({
    mutationFn: (status: 'Open' | 'Closed') =>
      unwrap(api.PATCH('/api/businesses/{businessId}/conversations/{id}/status', { params: { path: { businessId, id } }, body: { status } })),
    onSuccess: update,
  })
}
