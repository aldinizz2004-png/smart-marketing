import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { workspaceRoutes } from '@/test/fixtures'
import { fakeApi, renderAt, signIn, signOut } from '@/test/render'
import { AppRoutes } from './routes'

describe('app routes', () => {
  it('shows the home page with the Arabic navigation to a signed-in user', async () => {
    signIn()
    fakeApi(workspaceRoutes())
    renderAt(<AppRoutes />)

    expect(await screen.findByRole('heading', { level: 1, name: /أهلاً يا Sara/ })).toBeInTheDocument()
    const nav = screen.getAllByRole('navigation', { name: 'القائمة الرئيسية' })[0]!
    expect(within(nav).getByRole('link', { name: 'التعليقات' })).toHaveAttribute('href', '/comments')
  })

  it('reports that the API is reachable', async () => {
    signIn()
    fakeApi(workspaceRoutes())
    renderAt(<AppRoutes />)

    expect(await screen.findByText('متصل بالخادم')).toBeInTheDocument()
  })

  it('switches the whole page to English', async () => {
    signIn()
    fakeApi(workspaceRoutes())
    renderAt(<AppRoutes />)

    await userEvent.click(await screen.findByRole('button', { name: 'التبديل إلى الإنجليزية' }))

    expect(await screen.findByRole('heading', { level: 1, name: /Welcome, Sara/ })).toBeInTheDocument()
    expect(document.documentElement.dir).toBe('ltr')
  })

  it('sends visitors who are not signed in to the login page', () => {
    signOut()
    renderAt(<AppRoutes />, '/comments')

    expect(screen.getByRole('heading', { name: 'تسجيل الدخول' })).toBeInTheDocument()
  })

  it('shows a friendly page for unknown links', async () => {
    signIn()
    fakeApi(workspaceRoutes())
    renderAt(<AppRoutes />, '/no-such-page')

    expect(await screen.findByRole('heading', { name: 'الصفحة غير موجودة' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'العودة للرئيسية' })).toHaveAttribute('href', '/')
  })
})
