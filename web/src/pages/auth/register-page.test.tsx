import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { AppRoutes } from '@/app/routes'
import { fakeApi, renderAt, signOut } from '@/test/render'

describe('register page', () => {
  it('asks for a stronger password before sending', async () => {
    signOut()
    const { calls } = fakeApi([])
    renderAt(<AppRoutes />, '/register')

    await userEvent.type(screen.getByLabelText('اسمك الكامل'), 'Sara Haddad')
    await userEvent.type(screen.getByLabelText('اسم شركتك أو محلك'), 'Sara Cafe')
    await userEvent.type(screen.getByLabelText('البريد الإلكتروني'), 'sara@test.local')
    await userEvent.type(screen.getByLabelText('كلمة المرور'), 'onlyletters')
    await userEvent.click(screen.getByRole('button', { name: 'إنشاء الحساب' }))

    expect(await screen.findByText('كلمة المرور تحتاج حرفاً ورقماً على الأقل')).toBeInTheDocument()
    expect(calls).toHaveLength(0)
  })

  it('shows the API answer when the email is taken', async () => {
    signOut()
    fakeApi([
      { method: 'POST', path: '/api/auth/register', status: 409, body: { title: 'Conflict', detail: 'An account with this email already exists.' } },
    ])
    renderAt(<AppRoutes />, '/register')

    await userEvent.type(screen.getByLabelText('اسمك الكامل'), 'Sara Haddad')
    await userEvent.type(screen.getByLabelText('اسم شركتك أو محلك'), 'Sara Cafe')
    await userEvent.type(screen.getByLabelText('البريد الإلكتروني'), 'sara@test.local')
    await userEvent.type(screen.getByLabelText('كلمة المرور'), 'Passw0rd1')
    await userEvent.click(screen.getByRole('button', { name: 'إنشاء الحساب' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('An account with this email already exists.')
  })
})
