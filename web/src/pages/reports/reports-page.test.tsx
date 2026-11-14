import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { AppRoutes } from '@/app/routes'
import { paged, workspaceRoutes } from '@/test/fixtures'
import { fakeApi, renderAt, signIn } from '@/test/render'

const summary = {
  id: 'rep1',
  periodStart: '2026-11-02T00:00:00Z',
  periodEnd: '2026-11-09T00:00:00Z',
  headline: 'A strong week for the pistachio latte',
  isScheduled: true,
  sentAt: '2026-11-09T07:00:00Z',
  createdAt: '2026-11-09T06:00:00Z',
}

const report = {
  ...summary,
  businessId: 'b1',
  summary: 'Reach grew while engagement stayed steady.',
  wins: ['Reels reached twice as many people'],
  concerns: ['Two complaints are still waiting for a reply'],
  nextActions: ['Reply to the open complaints'],
  sentTo: 'sara@test.local',
  facts: {
    posts: 6,
    previousPosts: 4,
    reach: 12400,
    previousReach: 10000,
    engagementRate: 0.047,
    previousEngagementRate: 0.05,
    bestPlatform: { key: 'instagram', posts: 6, engagementRate: 0.047 },
    bestFormat: { key: 'Reel', posts: 2, engagementRate: 0.061 },
    bestHook: { key: 'None', posts: 1, engagementRate: 0.02 },
    topPost: { platform: 'instagram', format: 'Reel', hook: 'Question', reach: 5200, engagementRate: 0.07, url: 'https://www.instagram.com/p/abc', excerpt: 'Try our new latte' },
    newComments: 9,
    commentsByCategory: { Question: 4, Praise: 5, Spam: 0 },
    unansweredComplaints: 2,
    unansweredQuestions: 1,
    awaitingReview: 3,
    approvedNotScheduled: 1,
    scheduledNext7Days: 5,
    offersEndingSoon: ['Weekend coffee deal'],
  },
}

function routes(extra: Parameters<typeof fakeApi>[0] = []) {
  return [
    ...extra,
    ...workspaceRoutes(),
    { path: '/api/businesses/b1/reports', body: paged([summary]) },
    { path: '/api/businesses/b1/reports/rep1', body: report },
  ]
}

describe('reports list', () => {
  it('lists the reports with their delivery state', async () => {
    signIn('Owner')
    fakeApi(routes())
    renderAt(<AppRoutes />, '/reports')

    const card = (await screen.findByRole('heading', { name: 'A strong week for the pistachio latte' })).closest('a')!
    expect(card).toHaveAttribute('href', '/reports/rep1')
    expect(within(card).getByText('أسبوعي تلقائي')).toBeInTheDocument()
    expect(within(card).getByText(/أُرسل/)).toBeInTheDocument()
  })

  it('shows an empty state', async () => {
    signIn('Owner')
    fakeApi(routes([{ path: '/api/businesses/b1/reports', body: paged([]) }]))
    renderAt(<AppRoutes />, '/reports')

    expect(await screen.findByText('ما في تقارير لسا')).toBeInTheDocument()
  })

  it('creates a report for the chosen period and opens it', async () => {
    signIn('Owner')
    const { calls } = fakeApi(routes([{ method: 'POST', path: '/api/businesses/b1/reports', status: 201, body: report }]))
    renderAt(<AppRoutes />, '/reports')

    await userEvent.click((await screen.findAllByRole('button', { name: 'تقرير جديد' }))[0]!)
    const dialog = await screen.findByRole('dialog')
    await userEvent.selectOptions(within(dialog).getByLabelText('الفترة'), '14')
    await userEvent.click(within(dialog).getByLabelText('ابعته بالإيميل كمان'))
    await userEvent.click(within(dialog).getByRole('button', { name: 'أنشئ التقرير' }))

    await waitFor(() => expect(calls.find((c) => c.method === 'POST')?.body).toEqual({ days: 14, send: true }))
    expect(await screen.findByRole('heading', { name: 'A strong week for the pistachio latte', level: 1 })).toBeInTheDocument()
  })

  it('does not offer the email option to an editor', async () => {
    signIn('Editor')
    fakeApi(routes())
    renderAt(<AppRoutes />, '/reports')

    await userEvent.click((await screen.findAllByRole('button', { name: 'تقرير جديد' }))[0]!)
    const dialog = await screen.findByRole('dialog')
    expect(within(dialog).queryByLabelText('ابعته بالإيميل كمان')).not.toBeInTheDocument()
  })

  it('lets a viewer read but not create', async () => {
    signIn('Viewer')
    fakeApi(routes())
    renderAt(<AppRoutes />, '/reports')

    expect(await screen.findByRole('heading', { name: 'A strong week for the pistachio latte' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'تقرير جديد' })).not.toBeInTheDocument()
  })
})

describe('report page', () => {
  it('shows the summary, advice, numbers and what needs attention', async () => {
    signIn('Owner')
    fakeApi(routes())
    renderAt(<AppRoutes />, '/reports/rep1')

    expect(await screen.findByText('Reach grew while engagement stayed steady.')).toBeInTheDocument()
    expect(screen.getByText('Reels reached twice as many people')).toBeInTheDocument()
    expect(screen.getByText('Two complaints are still waiting for a reply')).toBeInTheDocument()
    expect(screen.getByText('Reply to the open complaints')).toBeInTheDocument()

    const reach = screen.getByText('الوصول', { selector: 'dt' }).closest('div')!
    expect(reach).toHaveTextContent('12,400')
    expect(reach).toHaveTextContent(/\+24/)
    expect(screen.getByText('Weekend coffee deal')).toBeInTheDocument()
    expect(screen.getByText('Try our new latte')).toBeInTheDocument()
  })

  it('opens the email version in a locked frame', async () => {
    signIn('Owner')
    fakeApi(routes([{ path: '/api/businesses/b1/reports/rep1/html', body: '<p>Hello owner</p>', headers: { 'Content-Type': 'text/html' } }]))
    renderAt(<AppRoutes />, '/reports/rep1')

    await userEvent.click(await screen.findByRole('button', { name: 'شوف نسخة الإيميل' }))
    const frame = await screen.findByTitle('نسخة الإيميل')
    expect(frame).toHaveAttribute('sandbox', '')
    await waitFor(() => expect(frame.getAttribute('srcdoc')).toContain('Hello owner'))
  })

  it('sends the report after confirming', async () => {
    signIn('Owner')
    const { calls } = fakeApi(routes([{ method: 'POST', path: '/api/businesses/b1/reports/rep1/send', body: { ...report, sentTo: 'sara@test.local' } }]))
    renderAt(<AppRoutes />, '/reports/rep1')

    await userEvent.click(await screen.findByRole('button', { name: 'ابعت مرة ثانية' }))
    const dialog = await screen.findByRole('dialog')
    await userEvent.click(within(dialog).getByRole('button', { name: 'ابعت بالإيميل' }))

    await waitFor(() => expect(calls.some((c) => c.method === 'POST' && c.url.endsWith('/send'))).toBe(true))
    expect(await screen.findByText('تم إرسال التقرير إلى sara@test.local')).toBeInTheDocument()
  })

  it('shows the API reason when sending fails', async () => {
    signIn('Owner')
    fakeApi(routes([{ method: 'POST', path: '/api/businesses/b1/reports/rep1/send', status: 502, body: { title: 'Bad Gateway', detail: 'The mail server did not answer.' } }]))
    renderAt(<AppRoutes />, '/reports/rep1')

    await userEvent.click(await screen.findByRole('button', { name: 'ابعت مرة ثانية' }))
    await userEvent.click(within(await screen.findByRole('dialog')).getByRole('button', { name: 'ابعت بالإيميل' }))
    expect(await screen.findByText('The mail server did not answer.')).toBeInTheDocument()
  })

  it('hides sending from an editor', async () => {
    signIn('Editor')
    fakeApi(routes())
    renderAt(<AppRoutes />, '/reports/rep1')

    expect(await screen.findByText('Reach grew while engagement stayed steady.')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /ابعت/ })).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'شوف نسخة الإيميل' })).toBeInTheDocument()
  })
})
