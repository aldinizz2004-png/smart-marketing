import { describe, expect, it } from 'vitest'
import { api } from '@/api/client'
import { fakeApi, signIn } from '@/test/render'
import { sessionStore } from './session'

const fresh = {
  accessToken: 'new-token',
  expiresAt: new Date(Date.now() + 15 * 60_000).toISOString(),
  user: { id: 'u1', email: 'sara@test.local', fullName: 'Sara', preferredLanguage: 'ar' },
  tenant: { id: 't1', name: 'Sara Cafe', role: 'Owner' },
}

describe('authFetch', () => {
  it('sends the access token', async () => {
    signIn()
    const { calls } = fakeApi([{ path: '/api/me', body: { id: 'u1' } }])

    await api.GET('/api/me')

    expect(calls[0]!.headers.get('Authorization')).toBe('Bearer test-token')
  })

  it('refreshes once on 401 and repeats the request with the new token', async () => {
    signIn()
    let meCalls = 0
    const { calls, fetchMock } = fakeApi([{ method: 'POST', path: '/api/auth/refresh', body: fresh }])
    const inner = fetchMock.getMockImplementation()!
    fetchMock.mockImplementation(async (input, init) => {
      const url = input instanceof Request ? input.url : String(input)
      if (url.endsWith('/api/me')) {
        meCalls++
        const request = input as Request
        const authorized = request.headers.get('Authorization') === 'Bearer new-token'
        return new Response(JSON.stringify({ id: 'u1' }), { status: authorized ? 200 : 401 })
      }
      return inner(input, init)
    })

    const { response } = await api.GET('/api/me')

    expect(response.status).toBe(200)
    expect(meCalls).toBe(2)
    expect(calls.find((c) => c.url === '/api/auth/refresh')!.headers.get('X-Auth-Transport')).toBe('cookie')
    expect(sessionStore.accessToken()).toBe('new-token')
  })

  it('signs the user out when the refresh cookie is no longer valid', async () => {
    signIn()
    const { fetchMock } = fakeApi([{ method: 'POST', path: '/api/auth/refresh', status: 401, body: { title: 'Unauthorized' } }])
    const inner = fetchMock.getMockImplementation()!
    fetchMock.mockImplementation(async (input, init) => {
      const url = input instanceof Request ? input.url : String(input)
      if (url.endsWith('/api/me')) return new Response('{}', { status: 401 })
      return inner(input, init)
    })

    const { response } = await api.GET('/api/me')

    expect(response.status).toBe(401)
    expect(sessionStore.get().status).toBe('anonymous')
  })
})
