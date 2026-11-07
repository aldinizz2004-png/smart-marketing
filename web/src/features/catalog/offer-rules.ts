// The offer moves the API allows (OfferService.Flow); an ended offer is final.
export const offerStatuses = ['Draft', 'Active', 'Paused', 'Ended'] as const
export type OfferStatus = (typeof offerStatuses)[number]

export const offerNext: Record<OfferStatus, OfferStatus[]> = {
  Draft: ['Active', 'Ended'],
  Active: ['Paused', 'Ended'],
  Paused: ['Active', 'Ended'],
  Ended: [],
}

export const canEditOffer = (status?: string | null) => status !== 'Ended'
export const canDeleteOffer = (status?: string | null) => status === 'Draft'
