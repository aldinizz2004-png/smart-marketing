import createClient from 'openapi-fetch'
import i18n from '@/i18n'
import type { paths } from './schema'

export const apiBaseUrl = import.meta.env.VITE_API_URL ?? 'http://localhost:5233'

// Typed from the API's Swagger document (npm run api:types). credentials: 'include' sends the refresh cookie.
export const api = createClient<paths>({ baseUrl: apiBaseUrl, credentials: 'include' })

api.use({
  onRequest({ request }) {
    request.headers.set('Accept-Language', i18n.language)
    return request
  },
})
