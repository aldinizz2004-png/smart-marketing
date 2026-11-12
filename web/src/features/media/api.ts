import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api, apiBaseUrl } from '@/api/client'
import { ApiError, unwrap, unwrapEmpty, type ProblemBody } from '@/api/errors'
import type { components } from '@/api/schema'
import { authFetch } from '@/auth/auth-fetch'

export type MediaAsset = components['schemas']['MediaAssetResponse']
export type MediaKind = components['schemas']['MediaKind']

export const mediaKinds: MediaKind[] = ['ProductPhoto', 'BrandImage', 'Logo', 'GeneratedImage', 'Video', 'Document']
export const maxUploadBytes = 10 * 1024 * 1024

const keys = {
  all: (businessId: string) => ['media', businessId] as const,
  list: (businessId: string, kind?: string, productId?: string, page = 1) => ['media', businessId, 'list', kind ?? '', productId ?? '', page] as const,
}

export function useMedia(businessId: string, { kind, productId, page = 1, pageSize = 24 }: { kind?: MediaKind | ''; productId?: string; page?: number; pageSize?: number } = {}) {
  return useQuery({
    queryKey: keys.list(businessId, kind, productId, page),
    queryFn: () =>
      unwrap(
        api.GET('/api/businesses/{businessId}/media', {
          params: { path: { businessId }, query: { Kind: kind || undefined, ProductId: productId || undefined, Page: page, PageSize: pageSize } },
        }),
      ),
    placeholderData: keepPreviousData,
  })
}

// openapi-fetch sends JSON by default; uploads are multipart, so they go through authFetch directly.
export function useUploadMedia(businessId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({ file, kind, productId }: { file: File; kind: MediaKind; productId?: string | null }) => {
      const form = new FormData()
      form.append('file', file)
      form.append('kind', kind)
      if (productId) form.append('productId', productId)
      let response: Response
      try {
        response = await authFetch(new Request(`${apiBaseUrl}/api/businesses/${businessId}/media`, { method: 'POST', body: form, credentials: 'include' }))
      } catch (cause) {
        throw new ApiError(0, { detail: cause instanceof Error ? cause.message : null })
      }
      const body = (await response.json().catch(() => null)) as unknown
      if (!response.ok) throw new ApiError(response.status, body as ProblemBody)
      return body as MediaAsset
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: keys.all(businessId) }),
  })
}

export function useDescribeMedia(businessId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, force }: { id: string; force?: boolean }) =>
      unwrap(api.POST('/api/businesses/{businessId}/media/{id}/describe', { params: { path: { businessId, id }, query: { force } } })),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: keys.all(businessId) }),
  })
}

export function useDeleteMedia(businessId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => unwrapEmpty(api.DELETE('/api/businesses/{businessId}/media/{id}', { params: { path: { businessId, id } } })),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: keys.all(businessId) }),
  })
}

// Media needs the access token, so <img src> cannot load it directly: fetch it once and use a blob URL.
export function useMediaBlob(contentUrl: string | null | undefined) {
  return useQuery({
    queryKey: ['media-blob', contentUrl],
    queryFn: async () => {
      const url = contentUrl!.startsWith('http') ? contentUrl! : `${apiBaseUrl}${contentUrl}`
      const response = await authFetch(new Request(url, { credentials: 'include' }))
      if (!response.ok) throw new ApiError(response.status, null)
      return URL.createObjectURL(await response.blob())
    },
    enabled: Boolean(contentUrl),
    staleTime: Infinity,
    gcTime: 30 * 60_000,
  })
}
