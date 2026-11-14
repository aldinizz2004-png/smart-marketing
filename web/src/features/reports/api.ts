import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/api/client'
import { unwrap } from '@/api/errors'
import type { components } from '@/api/schema'

export type ReportSummary = components['schemas']['ReportSummaryResponse']
export type Report = components['schemas']['ReportResponse']
export type ReportFacts = components['schemas']['OwnerReportFacts']

export const reportDays = [7, 14, 30] as const

const keys = {
  all: (businessId: string) => ['reports', businessId] as const,
  item: (businessId: string, id: string) => ['reports', businessId, 'item', id] as const,
  html: (businessId: string, id: string) => ['reports', businessId, 'html', id] as const,
}

export function useReports(businessId: string, page: number) {
  return useQuery({
    queryKey: [...keys.all(businessId), 'list', page],
    queryFn: () => unwrap(api.GET('/api/businesses/{businessId}/reports', { params: { path: { businessId }, query: { Page: page, PageSize: 12 } } })),
    placeholderData: keepPreviousData,
  })
}

export function useReport(businessId: string, id: string) {
  return useQuery({
    queryKey: keys.item(businessId, id),
    queryFn: () => unwrap(api.GET('/api/businesses/{businessId}/reports/{id}', { params: { path: { businessId, id } } })),
  })
}

// The page that is mailed to the owner; it needs the access token, so it is fetched as text and shown in a sandboxed frame.
export function useReportHtml(businessId: string, id: string, enabled: boolean) {
  return useQuery({
    queryKey: keys.html(businessId, id),
    enabled,
    queryFn: () => unwrap(api.GET('/api/businesses/{businessId}/reports/{id}/html', { params: { path: { businessId, id } }, parseAs: 'text' })) as Promise<string>,
  })
}

export function useGenerateReport(businessId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ days, send }: { days: number; send: boolean }) => unwrap(api.POST('/api/businesses/{businessId}/reports', { params: { path: { businessId } }, body: { days, send } })),
    onSuccess: () => Promise.all([queryClient.invalidateQueries({ queryKey: keys.all(businessId) }), queryClient.invalidateQueries({ queryKey: ['dashboard'] })]),
  })
}

export function useSendReport(businessId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => unwrap(api.POST('/api/businesses/{businessId}/reports/{id}/send', { params: { path: { businessId, id } } })),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: keys.all(businessId) }),
  })
}

// Share of change against the previous period; null when there is nothing to compare with.
export function changeRatio(current?: number, previous?: number) {
  if (current == null || previous == null || previous === 0) return null
  return (current - previous) / previous
}
