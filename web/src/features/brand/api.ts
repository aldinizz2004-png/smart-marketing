import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/api/client'
import { ApiError, unwrap, unwrapEmpty } from '@/api/errors'
import type { components } from '@/api/schema'

export type BrandProfile = components['schemas']['BrandProfileResponse']
export type BrandProfileRequest = components['schemas']['BrandProfileRequest']
export type BrandTerm = components['schemas']['BrandTermResponse']
export type BrandTermRequest = components['schemas']['BrandTermRequest']

const keys = {
  profile: (businessId: string) => ['brand-profile', businessId] as const,
  terms: (businessId: string) => ['brand-terms', businessId] as const,
}

// 404 means the owner has not described the brand yet.
export function useBrandProfile(businessId: string) {
  return useQuery({
    queryKey: keys.profile(businessId),
    queryFn: async () => {
      try {
        return await unwrap(api.GET('/api/businesses/{businessId}/brand-profile', { params: { path: { businessId } } }))
      } catch (error) {
        if (error instanceof ApiError && error.status === 404) return null
        throw error
      }
    },
  })
}

export function useSaveBrandProfile(businessId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: BrandProfileRequest) =>
      unwrap(api.PUT('/api/businesses/{businessId}/brand-profile', { params: { path: { businessId } }, body })),
    onSuccess: (profile) => queryClient.setQueryData(keys.profile(businessId), profile),
  })
}

export function useBrandTerms(businessId: string) {
  return useQuery({
    queryKey: keys.terms(businessId),
    queryFn: () => unwrap(api.GET('/api/businesses/{businessId}/brand-terms', { params: { path: { businessId } } })),
  })
}

export function useAddBrandTerm(businessId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (body: BrandTermRequest) =>
      unwrap(api.POST('/api/businesses/{businessId}/brand-terms', { params: { path: { businessId } }, body })),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: keys.terms(businessId) }),
  })
}

export function useDeleteBrandTerm(businessId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (termId: string) =>
      unwrapEmpty(api.DELETE('/api/businesses/{businessId}/brand-terms/{termId}', { params: { path: { businessId, termId } } })),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: keys.terms(businessId) }),
  })
}
