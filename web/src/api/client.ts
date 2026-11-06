import createClient from 'openapi-fetch'
import { authFetch } from '@/auth/auth-fetch'
import i18n from '@/i18n'
import { apiBaseUrl } from './base-url'
import type { paths } from './schema'

export { apiBaseUrl }

// Typed from the API's Swagger document (npm run api:types). credentials: 'include' sends the refresh cookie.
export const api = createClient<paths>({ baseUrl: apiBaseUrl, credentials: 'include', fetch: authFetch })

api.use({
  onRequest({ request }) {
    request.headers.set('Accept-Language', i18n.language)
    return request
  },
})
