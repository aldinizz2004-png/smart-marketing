import { cleanup, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { changeLanguage } from '@/i18n'
import { renderAt } from '@/test/render'
import { AppRoutes } from './routes'

describe('app routes', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('{}', { status: 200 })))
  })

  afterEach(async () => {
    cleanup()
    vi.unstubAllGlobals()
    await changeLanguage('ar')
  })

  it('shows the home page with the Arabic navigation', () => {
    renderAt(<AppRoutes />)

    expect(screen.getByRole('heading', { level: 1, name: /أهلاً بك/ })).toBeInTheDocument()
    const nav = screen.getAllByRole('navigation', { name: 'القائمة الرئيسية' })[0]!
    expect(within(nav).getByRole('link', { name: 'التعليقات' })).toHaveAttribute('href', '/comments')
  })

  it('reports that the API is reachable', async () => {
    renderAt(<AppRoutes />)

    expect(await screen.findByText('متصل بالخادم')).toBeInTheDocument()
  })

  it('switches the whole page to English', async () => {
    renderAt(<AppRoutes />)

    await userEvent.click(screen.getByRole('button', { name: 'التبديل إلى الإنجليزية' }))

    expect(await screen.findByRole('heading', { level: 1, name: /Welcome/ })).toBeInTheDocument()
    expect(document.documentElement.dir).toBe('ltr')
  })

  it('shows a friendly page for unknown links', () => {
    renderAt(<AppRoutes />, '/no-such-page')

    expect(screen.getByRole('heading', { name: 'الصفحة غير موجودة' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'العودة للرئيسية' })).toHaveAttribute('href', '/')
  })
})
