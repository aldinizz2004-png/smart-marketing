import { QueryClientProvider } from '@tanstack/react-query'
import { render } from '@testing-library/react'
import type { ReactElement } from 'react'
import { MemoryRouter } from 'react-router'
import { sessionStore, type Role } from '@/auth/session'
import { ToastProvider } from '@/components/ui/toast'
import { createQueryClient } from '@/lib/query-client'

export function renderAt(ui: ReactElement, path = '/') {
  const queryClient = createQueryClient()
  queryClient.setDefaultOptions({ queries: { retry: false }, mutations: { retry: false } })
  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <MemoryRouter initialEntries={[path]}>{ui}</MemoryRouter>
      </ToastProvider>
    </QueryClientProvider>,
  )
}

export function signIn(role: Role = 'Owner', fullName = 'Sara Haddad') {
  sessionStore.set({
    status: 'authenticated',
    session: {
      accessToken: 'test-token',
      expiresAt: Date.now() + 15 * 60_000,
      user: { id: 'u1', email: 'sara@test.local', fullName, preferredLanguage: 'ar' },
      tenant: { id: 't1', name: 'Sara Cafe', role },
    },
  })
}

export function signOut() {
  sessionStore.set({ status: 'anonymous' })
}

type Route = { method?: string; path: string | RegExp; status?: number; body?: unknown; headers?: Record<string, string> }

// A fake API: every fetch is matched against the routes; unmatched calls fail the test loudly.
export function fakeApi(routes: Route[]) {
  const calls: { method: string; url: string; body: unknown; headers: Headers }[] = []
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const request = input instanceof Request ? input : new Request(String(input), init)
    const url = new URL(request.url)
    const text = await request.clone().text()
    calls.push({ method: request.method, url: url.pathname + url.search, body: text ? JSON.parse(text) : null, headers: request.headers })
    const route = routes.find(
      (r) => (r.method ?? 'GET') === request.method && (typeof r.path === 'string' ? r.path === url.pathname : r.path.test(url.pathname)),
    )
    if (!route) throw new Error(`No fake route for ${request.method} ${url.pathname}`)
    const status = route.status ?? 200
    return new Response(status === 204 ? null : JSON.stringify(route.body ?? {}), {
      status,
      headers: { 'Content-Type': 'application/json', ...route.headers },
    })
  })
  vi.stubGlobal('fetch', fetchMock)
  return { calls, fetchMock }
}
