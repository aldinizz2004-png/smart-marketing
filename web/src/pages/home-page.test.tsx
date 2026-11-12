import { screen, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { AppRoutes } from '@/app/routes'
import { dashboardRoutes, paged, workspaceRoutes } from '@/test/fixtures'
import { fakeApi, renderAt, signIn } from '@/test/render'

describe('home page', () => {
  it('shows what needs attention with links to it', async () => {
    signIn()
    fakeApi([
      ...workspaceRoutes(),
      { path: '/api/businesses/b1/content-items', body: { ...paged([]), totalCount: 2 } },
      { path: '/api/businesses/b1/comments', body: { ...paged([]), totalCount: 3 } },
      ...dashboardRoutes(),
    ])
    renderAt(<AppRoutes />)

    const review = await screen.findByRole('link', { name: /بانتظار موافقتك/ })
    expect(await within(review).findByText('2')).toBeInTheDocument()
    expect(review).toHaveAttribute('href', '/content?status=InReview')
    expect(screen.getByRole('link', { name: /شكاوى تحتاج رداً/ })).toHaveTextContent('3')
  })

  it('shows the setup checklist until the shop is ready', async () => {
    signIn()
    fakeApi([...workspaceRoutes(), ...dashboardRoutes()])
    renderAt(<AppRoutes />)

    expect(await screen.findByRole('heading', { name: 'أكمل إعداد محلك' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'اربط حساب تواصل' })).toHaveAttribute('href', '/settings/accounts')
  })

  it('lists the next scheduled posts', async () => {
    signIn()
    fakeApi([
      ...workspaceRoutes(),
      {
        path: '/api/businesses/b1/publication-jobs',
        body: paged([{ id: 'j1', contentItemId: 'c1', platformCode: 'instagram', scheduledAt: '2030-01-01T10:00:00Z', status: { code: 'Queued' } }]),
      },
      ...dashboardRoutes(),
    ])
    renderAt(<AppRoutes />)

    expect(await screen.findByRole('link', { name: /إنستغرام/ })).toHaveAttribute('href', '/content/c1')
  })
})
