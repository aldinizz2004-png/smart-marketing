import type { components } from '@/api/schema'

export type AuthResponse = components['schemas']['AuthResponse']
export type Role = components['schemas']['MembershipRole']

export type Session = {
  accessToken: string
  expiresAt: number
  user: { id: string; email: string; fullName: string | null; preferredLanguage: string }
  tenant: { id: string; name: string; role: Role }
}

export type SessionState =
  | { status: 'loading' }
  | { status: 'anonymous' }
  | { status: 'authenticated'; session: Session }

// The access token lives only in memory; the refresh token is an httpOnly cookie the page cannot read.
let state: SessionState = { status: 'loading' }
const listeners = new Set<() => void>()

export const sessionStore = {
  get: () => state,
  subscribe(listener: () => void) {
    listeners.add(listener)
    return () => {
      listeners.delete(listener)
    }
  },
  accessToken: () => (state.status === 'authenticated' ? state.session.accessToken : null),
  expiresAt: () => (state.status === 'authenticated' ? state.session.expiresAt : 0),
  set(next: SessionState) {
    state = next
    listeners.forEach((listener) => listener())
  },
}

export function toSession(response: AuthResponse): Session {
  return {
    accessToken: response.accessToken ?? '',
    expiresAt: response.expiresAt ? Date.parse(response.expiresAt) : Date.now(),
    user: {
      id: response.user?.id ?? '',
      email: response.user?.email ?? '',
      fullName: response.user?.fullName ?? null,
      preferredLanguage: response.user?.preferredLanguage ?? 'ar',
    },
    tenant: {
      id: response.tenant?.id ?? '',
      name: response.tenant?.name ?? '',
      role: response.tenant?.role ?? 'Viewer',
    },
  }
}
