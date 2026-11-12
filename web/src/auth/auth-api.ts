import { apiBaseUrl } from '@/api/base-url'
import { ApiError, type ProblemBody } from '@/api/errors'
import type { components } from '@/api/schema'
import i18n from '@/i18n'
import { sessionStore, toSession, type AuthResponse, type Session } from './session'

type LoginRequest = components['schemas']['LoginRequest']
type RegisterRequest = components['schemas']['RegisterRequest']

// Auth calls go straight to fetch: they must not go through the 401 -> refresh retry of the API client.
async function post(path: string, body?: unknown, accessToken?: string): Promise<Response> {
  const headers: Record<string, string> = {
    'X-Auth-Transport': 'cookie',
    'Accept-Language': i18n.language,
  }
  if (body !== undefined) headers['Content-Type'] = 'application/json'
  if (accessToken) headers.Authorization = `Bearer ${accessToken}`

  try {
    return await fetch(`${apiBaseUrl}${path}`, {
      method: 'POST',
      credentials: 'include',
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
    })
  } catch (cause) {
    throw new ApiError(0, { detail: cause instanceof Error ? cause.message : null })
  }
}

async function readJson(response: Response): Promise<unknown> {
  return response.json().catch(() => null)
}

// Not a secret: only says whether a refresh cookie was issued, so first-time visitors skip a refresh that must fail.
const signedInFlag = 'smp.signedIn'

function rememberSignedIn(value: boolean) {
  try {
    if (value) localStorage.setItem(signedInFlag, '1')
    else localStorage.removeItem(signedInFlag)
  } catch {
    // storage blocked: the refresh call is simply always tried
  }
}

export function maybeSignedIn() {
  try {
    return localStorage.getItem(signedInFlag) !== null
  } catch {
    return true
  }
}

function signOutLocally() {
  rememberSignedIn(false)
  sessionStore.set({ status: 'anonymous' })
}

async function readSession(response: Response): Promise<Session> {
  const body = await readJson(response)
  if (!response.ok) throw new ApiError(response.status, body as ProblemBody)
  const session = toSession(body as AuthResponse)
  rememberSignedIn(true)
  sessionStore.set({ status: 'authenticated', session })
  return session
}

export async function login(request: LoginRequest) {
  return readSession(await post('/api/auth/login', request))
}

export async function register(request: RegisterRequest) {
  return readSession(await post('/api/auth/register', request))
}

let refreshing: Promise<Session | null> | null = null

// One refresh at a time: parallel 401s wait for the same new token instead of burning the rotated cookie.
export function refreshSession(): Promise<Session | null> {
  refreshing ??= (async () => {
    try {
      return await readSession(await post('/api/auth/refresh'))
    } catch (error) {
      // Offline for a moment: keep the user signed in and let the next request try again.
      if (error instanceof ApiError && error.status === 0 && sessionStore.get().status === 'authenticated') return null
      signOutLocally()
      return null
    } finally {
      refreshing = null
    }
  })()
  return refreshing
}

export async function logout() {
  try {
    await post('/api/auth/logout')
  } finally {
    signOutLocally()
  }
}

// On page load: turn the refresh cookie into a session, or go straight to signed out for a first-time visitor.
export async function restoreSession() {
  if (!maybeSignedIn()) {
    sessionStore.set({ status: 'anonymous' })
    return
  }
  await refreshSession()
}

export async function logoutEverywhere(): Promise<number> {
  const response = await post('/api/auth/logout-all', undefined, sessionStore.accessToken() ?? undefined)
  const body = (await readJson(response)) as { sessionsEnded?: number } | null
  if (!response.ok) throw new ApiError(response.status, body as ProblemBody)
  await logout()
  return body?.sessionsEnded ?? 0
}
