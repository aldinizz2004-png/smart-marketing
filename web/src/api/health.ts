import { useQuery } from '@tanstack/react-query'
import { apiBaseUrl } from './client'

// /health is outside the Swagger document, so it is called with plain fetch.
async function fetchHealth(signal: AbortSignal) {
  const response = await fetch(`${apiBaseUrl}/health`, { signal })
  if (!response.ok) throw new Error(`Health check failed with ${response.status}`)
  return true
}

export function useApiHealth() {
  return useQuery({
    queryKey: ['health'],
    queryFn: ({ signal }) => fetchHealth(signal),
    refetchInterval: 60_000,
    retry: false,
  })
}
