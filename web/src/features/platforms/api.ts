import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { api } from '@/api/client'
import { unwrap } from '@/api/errors'
import type { components } from '@/api/schema'

export type Platform = components['schemas']['PlatformResponse']

export function usePlatforms() {
  const { i18n } = useTranslation()
  return useQuery({
    queryKey: ['platforms', i18n.language],
    queryFn: () => unwrap(api.GET('/api/platforms', { params: { query: { lang: i18n.language } } })),
    staleTime: 60 * 60_000,
  })
}

// Display name of a platform code, falling back to the code itself while the list loads.
export function usePlatformName() {
  const { data } = usePlatforms()
  return (code?: string | null) => data?.find((p) => p.code === code)?.displayName ?? code ?? ''
}
