import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { AppRoutes } from '@/app/routes'
import { renderAt, signIn, signOut } from '@/test/render'

describe('landing page', () => {
  it('greets a first-time visitor at the root', async () => {
    signOut()
    renderAt(<AppRoutes />, '/')

    expect(await screen.findByRole('heading', { level: 1, name: /سوشال ميديا محلك/ })).toBeInTheDocument()
    expect(screen.getAllByRole('link', { name: 'إنشاء حساب' })[0]).toHaveAttribute('href', '/register')
  })

  it('sends a returning visitor to sign in', async () => {
    localStorage.setItem('smp.signedIn', '1')
    signOut()
    renderAt(<AppRoutes />, '/')

    expect(await screen.findByRole('button', { name: 'دخول' })).toBeInTheDocument()
  })

  it('offers the dashboard to someone already signed in', async () => {
    signIn()
    renderAt(<AppRoutes />, '/welcome')

    expect((await screen.findAllByRole('link', { name: 'لوحة التحكم' }))[0]).toHaveAttribute('href', '/')
    expect(screen.queryByRole('link', { name: 'تسجيل الدخول' })).not.toBeInTheDocument()
  })

  it('opens the menu on small screens', async () => {
    signOut()
    renderAt(<AppRoutes />, '/welcome')

    const toggle = await screen.findByRole('button', { name: 'فتح القائمة' })
    expect(toggle).toHaveAttribute('aria-expanded', 'false')
    await userEvent.click(toggle)
    expect(screen.getByRole('button', { name: 'إغلاق القائمة' })).toHaveAttribute('aria-expanded', 'true')
  })
})
