import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { AppRoutes } from '@/app/routes'
import { paged, workspaceRoutes } from '@/test/fixtures'
import { fakeApi, renderAt, signIn } from '@/test/render'

const performance = {
  posts: 6,
  totals: { reach: 12400, impressions: 20100, likes: 530, comments: 41, shares: 12, saves: 30, linkClicks: 18, engagementRate: 0.047 },
  byPlatform: [{ key: 'instagram', posts: 6, averageReach: 2066, engagementRate: 0.047 }],
  byFormat: [{ key: 'Reel', posts: 2, averageReach: 3100, engagementRate: 0.061 }, { key: 'SingleImage', posts: 4, averageReach: 1500, engagementRate: 0.03 }],
  byHook: [{ key: 'Question', posts: 3, averageReach: 2500, engagementRate: 0.05 }],
  topPosts: [{ publicationId: 'pub1', contentItemId: 'c1', platformCode: 'instagram', format: 'Reel', hookType: 'Question', reach: 5200, engagementRate: 0.07, externalUrl: 'https://www.instagram.com/p/abc' }],
}

const recommendation = {
  id: 'r1',
  strategyRunId: 'run1',
  campaignId: null,
  actionType: 'NewContent',
  proposedPayload: { platform: 'instagram', format: 'Reel', hook: 'Question', captionIdea: 'Ask followers about their morning coffee' },
  proposedPublishAt: '2026-11-15T16:00:00Z',
  rationale: 'Questions get the most comments on your page.',
  confidence: 0.82,
  status: { id: 1201, code: 'Pending', text: 'بانتظار' },
  appliedAutomatically: false,
  evidence: [{ interpretation: 'Your last question post reached 5200 people.' }],
}

const campaign = { id: 'camp1', name: 'Winter menu', status: { code: 'Active', text: 'فعّالة' } }

const run = {
  id: 'run1',
  windowStart: '2026-11-01T00:00:00Z',
  windowEnd: '2026-11-08T00:00:00Z',
  status: { id: 1103, code: 'Completed', text: 'مكتمل' },
  completedAt: '2026-11-08T08:00:00Z',
  recommendations: [recommendation],
}

function routes(extra: Parameters<typeof fakeApi>[0] = []) {
  return [
    ...extra,
    ...workspaceRoutes(),
    { path: '/api/businesses/b1/performance', body: performance },
    { path: '/api/businesses/b1/recommendations', body: paged([recommendation]) },
    { path: '/api/businesses/b1/strategy-runs', body: paged([run]) },
    { path: '/api/businesses/b1/campaigns', body: paged([campaign]) },
  ]
}

describe('performance tab', () => {
  it('shows totals, breakdowns and the best posts', async () => {
    signIn('Owner')
    fakeApi(routes())
    renderAt(<AppRoutes />, '/insights')

    expect(await screen.findByText('12,400')).toBeInTheDocument()
    expect(screen.getByText(/4\.7/, { selector: 'dd' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'حسب نوع المحتوى' }).closest('div')).toHaveTextContent('ريل')
    const top = screen.getByRole('heading', { name: 'أفضل المنشورات' }).closest('section')!
    expect(within(top).getByText('سؤال')).toBeInTheDocument()
    expect(within(top).getByRole('link', { name: /افتح المنشور/ })).toHaveAttribute('href', 'https://www.instagram.com/p/abc')
  })

  it('asks for the chosen period', async () => {
    signIn('Owner')
    const { calls } = fakeApi(routes())
    renderAt(<AppRoutes />, '/insights')

    await userEvent.click(await screen.findByRole('button', { name: 'آخر 30 يوم' }))
    await waitFor(() => {
      const last = [...calls].reverse().find((c) => c.url.startsWith('/api/businesses/b1/performance'))!
      const q = new URLSearchParams(last.url.split('?')[1])
      const span = (new Date(q.get('To')!).getTime() - new Date(q.get('From')!).getTime()) / 86_400_000
      expect(Math.round(span)).toBe(30)
    })
  })

  it('shows an empty state without posts', async () => {
    signIn('Owner')
    fakeApi(routes([{ path: '/api/businesses/b1/performance', body: { posts: 0, totals: {} } }]))
    renderAt(<AppRoutes />, '/insights')

    expect(await screen.findByText('ما في منشورات منشورة بهاي الفترة')).toBeInTheDocument()
  })
})

describe('recommendations tab', () => {
  it('lists pending recommendations with their reasons', async () => {
    signIn('Owner')
    const { calls } = fakeApi(routes())
    renderAt(<AppRoutes />, '/insights?tab=recommendations')

    expect(await screen.findByText('Ask followers about their morning coffee')).toBeInTheDocument()
    expect(screen.getByText('Questions get the most comments on your page.')).toBeInTheDocument()
    expect(screen.getByText(/الثقة.*82/)).toBeInTheDocument()
    expect(screen.getByText('Your last question post reached 5200 people.')).toBeInTheDocument()
    expect(calls.some((c) => c.url.includes('/recommendations?') && c.url.includes('Status=Pending'))).toBe(true)
  })

  it('accepts and rejects', async () => {
    signIn('Owner')
    const { calls } = fakeApi(routes([{ method: 'PATCH', path: '/api/businesses/b1/recommendations/r1/status', body: { ...recommendation, status: { code: 'Accepted' } } }]))
    renderAt(<AppRoutes />, '/insights?tab=recommendations')

    await userEvent.click(await screen.findByRole('button', { name: 'اقبل' }))
    await waitFor(() => expect(calls.find((c) => c.method === 'PATCH')?.body).toEqual({ status: 'Accepted' }))
    expect(await screen.findByText('تم قبول التوصية')).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'ارفض' }))
    await waitFor(() => expect(calls.filter((c) => c.method === 'PATCH').at(-1)?.body).toEqual({ status: 'Rejected' }))
  })

  it('needs a campaign to write the post when the recommendation has none', async () => {
    signIn('Owner')
    const accepted = { ...recommendation, status: { id: 1202, code: 'Accepted', text: 'مقبولة' } }
    const { calls } = fakeApi(
      routes([
        { path: '/api/businesses/b1/recommendations', body: paged([accepted]) },
        { method: 'POST', path: '/api/businesses/b1/recommendations/r1/apply', status: 201, body: { id: 'ci1', campaignId: 'camp1' } },
        { path: '/api/businesses/b1/content-items/ci1', body: { id: 'ci1', status: { code: 'Draft' }, revisions: [] } },
      ]),
    )
    renderAt(<AppRoutes />, '/insights?tab=recommendations')

    await userEvent.click(await screen.findByRole('button', { name: 'اكتب المنشور' }))
    const dialog = await screen.findByRole('dialog')
    const submit = within(dialog).getByRole('button', { name: 'اكتب المنشور' })
    expect(submit).toBeDisabled()
    await userEvent.selectOptions(within(dialog).getByLabelText('الحملة'), 'camp1')
    await userEvent.click(submit)

    await waitFor(() => expect(calls.find((c) => c.method === 'POST')?.body).toEqual({ campaignId: 'camp1' }))
  })

  it('writes the post straight away when the recommendation already has a campaign', async () => {
    signIn('Owner')
    const accepted = { ...recommendation, campaignId: 'camp1', status: { id: 1202, code: 'Accepted', text: 'مقبولة' } }
    const { calls } = fakeApi(
      routes([
        { path: '/api/businesses/b1/recommendations', body: paged([accepted]) },
        { method: 'POST', path: '/api/businesses/b1/recommendations/r1/apply', status: 201, body: { id: 'ci1' } },
        { path: '/api/businesses/b1/content-items/ci1', body: { id: 'ci1', status: { code: 'Draft' }, revisions: [] } },
      ]),
    )
    renderAt(<AppRoutes />, '/insights?tab=recommendations')

    await userEvent.click(await screen.findByRole('button', { name: 'اكتب المنشور' }))
    const dialog = await screen.findByRole('dialog')
    expect(within(dialog).queryByLabelText('الحملة')).not.toBeInTheDocument()
    await userEvent.click(within(dialog).getByRole('button', { name: 'اكتب المنشور' }))

    await waitFor(() => expect(calls.find((c) => c.method === 'POST')?.body).toEqual({ campaignId: null }))
  })

  it('hides the decisions from a viewer', async () => {
    signIn('Viewer')
    fakeApi(routes())
    renderAt(<AppRoutes />, '/insights?tab=recommendations')

    expect(await screen.findByText('Ask followers about their morning coffee')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'اقبل' })).not.toBeInTheDocument()
  })

  it('filters by status', async () => {
    signIn('Owner')
    const { calls } = fakeApi(routes())
    renderAt(<AppRoutes />, '/insights?tab=recommendations')

    await userEvent.click(await screen.findByRole('button', { name: 'مقبولة' }))
    await waitFor(() => expect(calls.some((c) => c.url.includes('/recommendations?') && c.url.includes('Status=Accepted'))).toBe(true))
  })
})

describe('analyses tab', () => {
  it('lists earlier analyses', async () => {
    signIn('Owner')
    fakeApi(routes())
    renderAt(<AppRoutes />, '/insights?tab=runs')

    expect(await screen.findByText('1 توصية')).toBeInTheDocument()
    expect(screen.getByText('مكتمل')).toBeInTheDocument()
  })

  it('starts an analysis for a chosen period and opens the recommendations', async () => {
    signIn('Owner')
    const { calls } = fakeApi(routes([{ method: 'POST', path: '/api/businesses/b1/strategy-runs', status: 201, body: run }]))
    renderAt(<AppRoutes />, '/insights?tab=runs')

    await userEvent.selectOptions(await screen.findByLabelText('فترة التحليل'), '14')
    await userEvent.click(screen.getByRole('button', { name: 'ابدأ التحليل' }))

    await waitFor(() => expect(calls.find((c) => c.method === 'POST')?.body).toEqual({ windowDays: 14, campaignId: null }))
    expect(await screen.findByRole('tab', { name: 'التوصيات', selected: true })).toBeInTheDocument()
  })
})
