import { QueryClient } from '@tanstack/react-query'
import { ApiError } from '@/api/errors'

// 4xx answers will not change by asking again; network and server errors get two more tries.
export function shouldRetry(failureCount: number, error: unknown) {
  if (error instanceof ApiError && error.status >= 400 && error.status < 500) return false
  return failureCount < 2
}

export function createQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: { staleTime: 30_000, refetchOnWindowFocus: false, retry: shouldRetry },
      mutations: { retry: false },
    },
  })
}
