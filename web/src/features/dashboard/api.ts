import { useQuery } from '@tanstack/react-query'
import { api } from '@/api/client'
import { unwrap } from '@/api/errors'

const daysAgo = (days: number) => new Date(Date.now() - days * 86_400_000).toISOString()

// Counts only: PageSize 1 is enough because the API returns totalCount.
export function useAttentionCounts(businessId: string) {
  return useQuery({
    queryKey: ['dashboard', 'attention', businessId],
    queryFn: async () => {
      const path = { businessId }
      const [review, complaints, questions, waiting, scheduled] = await Promise.all([
        unwrap(api.GET('/api/businesses/{businessId}/content-items', { params: { path, query: { Status: 'InReview', PageSize: 1 } } })),
        unwrap(api.GET('/api/businesses/{businessId}/comments', { params: { path, query: { Category: 'Complaint', NeedsReply: true, Status: 'Analyzed', PageSize: 1 } } })),
        unwrap(api.GET('/api/businesses/{businessId}/comments', { params: { path, query: { NeedsReply: true, Status: 'Analyzed', PageSize: 1 } } })),
        unwrap(api.GET('/api/businesses/{businessId}/conversations', { params: { path, query: { Status: 'WaitingForOwner', PageSize: 1 } } })),
        unwrap(api.GET('/api/businesses/{businessId}/publication-jobs', { params: { path, query: { Status: 'Queued', PageSize: 1 } } })),
      ])
      return {
        awaitingReview: review.totalCount ?? 0,
        complaints: complaints.totalCount ?? 0,
        commentsToAnswer: questions.totalCount ?? 0,
        conversationsWaiting: waiting.totalCount ?? 0,
        scheduled: scheduled.totalCount ?? 0,
      }
    },
    refetchInterval: 60_000,
  })
}

export function useWeekPerformance(businessId: string) {
  return useQuery({
    queryKey: ['dashboard', 'performance', businessId],
    queryFn: () =>
      unwrap(api.GET('/api/businesses/{businessId}/performance', { params: { path: { businessId }, query: { From: daysAgo(7), To: new Date().toISOString() } } })),
  })
}

export function useUpcomingJobs(businessId: string) {
  return useQuery({
    queryKey: ['dashboard', 'upcoming', businessId],
    queryFn: async () => {
      const page = await unwrap(
        api.GET('/api/businesses/{businessId}/publication-jobs', { params: { path: { businessId }, query: { Status: 'Queued', PageSize: 50 } } }),
      )
      return (page.items ?? [])
        .filter((job) => job.scheduledAt)
        .sort((a, b) => Date.parse(a.scheduledAt!) - Date.parse(b.scheduledAt!))
        .slice(0, 5)
    },
  })
}

export function useLatestReport(businessId: string) {
  return useQuery({
    queryKey: ['dashboard', 'report', businessId],
    queryFn: async () => {
      const page = await unwrap(api.GET('/api/businesses/{businessId}/reports', { params: { path: { businessId }, query: { PageSize: 1 } } }))
      return page.items?.[0] ?? null
    },
  })
}

export function useSetupProgress(businessId: string) {
  return useQuery({
    queryKey: ['dashboard', 'setup', businessId],
    queryFn: async () => {
      const path = { businessId }
      const [brand, accounts, products] = await Promise.all([
        api.GET('/api/businesses/{businessId}/brand-profile', { params: { path } }),
        unwrap(api.GET('/api/businesses/{businessId}/social-accounts', { params: { path } })),
        unwrap(api.GET('/api/businesses/{businessId}/products', { params: { path, query: { PageSize: 1 } } })),
      ])
      return {
        brand: brand.response.ok,
        account: accounts.some((a) => a.status?.code === 'Connected'),
        product: (products.totalCount ?? 0) > 0,
      }
    },
  })
}
