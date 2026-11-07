import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/api/client'
import { unwrap, unwrapEmpty } from '@/api/errors'
import type { components } from '@/api/schema'

export type Product = components['schemas']['ProductResponse']
export type ProductRequest = components['schemas']['ProductRequest']
export type Offer = components['schemas']['OfferResponse']
export type OfferRequest = components['schemas']['OfferRequest']

const keys = {
  products: (businessId: string) => ['products', businessId] as const,
  offers: (businessId: string) => ['offers', businessId] as const,
}

export function useProducts(businessId: string, { search, page = 1, pageSize = 50, activeOnly }: { search?: string; page?: number; pageSize?: number; activeOnly?: boolean } = {}) {
  return useQuery({
    queryKey: [...keys.products(businessId), search ?? '', page, pageSize, activeOnly ?? null],
    queryFn: () =>
      unwrap(
        api.GET('/api/businesses/{businessId}/products', {
          params: { path: { businessId }, query: { Search: search || undefined, IsActive: activeOnly ? true : undefined, Page: page, PageSize: pageSize } },
        }),
      ),
    placeholderData: keepPreviousData,
  })
}

export function useSaveProduct(businessId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, body }: { id?: string; body: ProductRequest }) =>
      id
        ? unwrap(api.PUT('/api/businesses/{businessId}/products/{id}', { params: { path: { businessId, id } }, body }))
        : unwrap(api.POST('/api/businesses/{businessId}/products', { params: { path: { businessId } }, body })),
    onSuccess: () => Promise.all([queryClient.invalidateQueries({ queryKey: keys.products(businessId) }), queryClient.invalidateQueries({ queryKey: ['dashboard'] })]),
  })
}

export function useDeleteProduct(businessId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => unwrapEmpty(api.DELETE('/api/businesses/{businessId}/products/{id}', { params: { path: { businessId, id } } })),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: keys.products(businessId) }),
  })
}

export function useOffers(businessId: string, { status, page = 1, pageSize = 50 }: { status?: string; page?: number; pageSize?: number } = {}) {
  return useQuery({
    queryKey: [...keys.offers(businessId), status ?? '', page, pageSize],
    queryFn: () =>
      unwrap(api.GET('/api/businesses/{businessId}/offers', { params: { path: { businessId }, query: { Status: status || undefined, Page: page, PageSize: pageSize } } })),
    placeholderData: keepPreviousData,
  })
}

export function useSaveOffer(businessId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, body }: { id?: string; body: OfferRequest }) =>
      id
        ? unwrap(api.PUT('/api/businesses/{businessId}/offers/{id}', { params: { path: { businessId, id } }, body }))
        : unwrap(api.POST('/api/businesses/{businessId}/offers', { params: { path: { businessId } }, body })),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: keys.offers(businessId) }),
  })
}

export function useChangeOfferStatus(businessId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, status }: { id: string; status: string }) =>
      unwrap(api.PATCH('/api/businesses/{businessId}/offers/{id}/status', { params: { path: { businessId, id } }, body: { status } })),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: keys.offers(businessId) }),
  })
}

export function useDeleteOffer(businessId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => unwrapEmpty(api.DELETE('/api/businesses/{businessId}/offers/{id}', { params: { path: { businessId, id } } })),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: keys.offers(businessId) }),
  })
}
