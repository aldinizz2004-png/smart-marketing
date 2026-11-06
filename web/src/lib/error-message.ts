import type { TFunction } from 'i18next'
import { ApiError } from '@/api/errors'

export type ErrorText = { title: string; detail: string | null }

// The API writes its messages in English; Arabic users get an Arabic title and the API's detail under it.
export function describeError(error: unknown, t: TFunction): ErrorText {
  if (!(error instanceof ApiError)) return { title: t('errors.unexpected'), detail: null }

  const detail = error.firstFieldError() ?? error.detail
  switch (error.status) {
    case 0:
      return { title: t('errors.network'), detail: null }
    case 400:
      return { title: t('errors.badRequest'), detail }
    case 401:
      return { title: t('errors.unauthorized'), detail: null }
    case 402:
      return { title: t('errors.paymentRequired'), detail: null }
    case 403:
      return { title: t('errors.forbidden'), detail: null }
    case 404:
      return { title: t('errors.notFound'), detail: null }
    case 409:
      return { title: t('errors.conflict'), detail }
    case 413:
      return { title: t('errors.tooLarge'), detail }
    case 415:
      return { title: t('errors.unsupportedFile'), detail }
    case 429:
      return {
        title: t('errors.tooMany'),
        detail: error.retryAfterSeconds ? t('errors.retryAfter', { seconds: error.retryAfterSeconds }) : null,
      }
    case 502:
      return { title: t('errors.provider'), detail }
    default:
      return { title: t('errors.server'), detail: null }
  }
}
