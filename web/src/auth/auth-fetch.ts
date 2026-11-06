import { refreshSession } from './auth-api'
import { sessionStore } from './session'

const refreshEarlyMs = 30_000

// fetch for the API client: adds the access token, renews it shortly before it expires,
// and when the API still answers 401 it refreshes once and repeats the request.
export async function authFetch(request: Request): Promise<Response> {
  const retry = request.clone()

  if (sessionStore.accessToken() && sessionStore.expiresAt() - Date.now() < refreshEarlyMs) await refreshSession()

  const token = sessionStore.accessToken()
  if (token) request.headers.set('Authorization', `Bearer ${token}`)
  const response = await fetch(request)
  if (response.status !== 401 || !token) return response

  const session = await refreshSession()
  if (!session) return response
  retry.headers.set('Authorization', `Bearer ${session.accessToken}`)
  return fetch(retry)
}
