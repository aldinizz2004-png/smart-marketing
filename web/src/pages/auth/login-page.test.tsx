import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { AppRoutes } from '@/app/routes'
import { sessionStore } from '@/auth/session'
import { workspaceRoutes } from '@/test/fixtures'
import { fakeApi, renderAt, signOut } from '@/test/render'

const authResponse = {
  accessToken: 'token',
  expiresAt: new Date(Date.now() + 15 * 60_000).toISOString(),
  user: { id: 'u1', email: 'sara@test.local', fullName: 'Sara Haddad', preferredLanguage: 'ar' },
  tenant: { id: 't1', name: 'Sara Cafe', role: 'Owner' },
}

describe('login page', () => {
  it('checks the fields before sending anything', async () => {
    signOut()
    const { calls } = fakeApi([])
    renderAt(<AppRoutes />, '/login')

    await userEvent.click(screen.getByRole('button', { name: 'دخول' }))

    expect(await screen.findAllByText('هذا الحقل مطلوب')).toHaveLength(2)
    expect(calls).toHaveLength(0)
  })

  it('logs in with the cookie transport and opens the home page', async () => {
    signOut()
    const { calls } = fakeApi([
      { method: 'POST', path: '/api/auth/login', body: authResponse },
      ...workspaceRoutes(),
    ])
    renderAt(<AppRoutes />, '/login')

    await userEvent.type(screen.getByLabelText('البريد الإلكتروني'), 'sara@test.local')
    await userEvent.type(screen.getByLabelText('كلمة المرور'), 'Passw0rd!234')
    await userEvent.click(screen.getByRole('button', { name: 'دخول' }))

    expect(await screen.findByRole('heading', { level: 1, name: /أهلاً يا Sara/ })).toBeInTheDocument()
    const login = calls.find((c) => c.url === '/api/auth/login')!
    expect(login.body).toEqual({ email: 'sara@test.local', password: 'Passw0rd!234' })
    expect(login.headers.get('X-Auth-Transport')).toBe('cookie')
    expect(sessionStore.accessToken()).toBe('token')
  })

  it('says so when the password is wrong', async () => {
    signOut()
    fakeApi([{ method: 'POST', path: '/api/auth/login', status: 401, body: { title: 'Unauthorized', detail: 'Invalid email or password.' } }])
    renderAt(<AppRoutes />, '/login')

    await userEvent.type(screen.getByLabelText('البريد الإلكتروني'), 'sara@test.local')
    await userEvent.type(screen.getByLabelText('كلمة المرور'), 'wrong')
    await userEvent.click(screen.getByRole('button', { name: 'دخول' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('البريد أو كلمة المرور غير صحيحة')
  })
})
