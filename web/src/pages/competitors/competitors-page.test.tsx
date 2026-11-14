import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { AppRoutes } from '@/app/routes'
import { paged, workspaceRoutes } from '@/test/fixtures'
import { fakeApi, renderAt, signIn } from '@/test/render'

const source = {
  id: 'ts1',
  url: 'https://www.instagram.com/rival_cafe',
  platform: 'instagram',
  handle: 'rival_cafe',
  displayName: 'Rival Cafe',
  kind: 'Competitor',
  notes: 'Next street',
  status: { id: 1, code: 'Active', text: 'فعّال' },
  posts: 12,
  lastScannedAt: '2026-11-10T10:00:00Z',
  createdAt: '2026-11-01T10:00:00Z',
}

const insight = {
  id: 'i1',
  trackedSourceId: 'ts1',
  type: 'PostingTime',
  summary: 'They post at 7 PM and get the most likes.',
  applyToUs: 'Move your evening post to 7 PM.',
  confidence: 0.8,
}

const post = {
  id: 'p1',
  postUrl: 'https://www.instagram.com/p/abc',
  postedAt: '2026-11-09T19:00:00Z',
  format: 'Reel',
  hookType: 'Question',
  caption: 'Try our new latte',
  likes: 120,
  comments: 8,
  views: 3400,
  engagementRate: 0.052,
}

function routes(sources = [source], insights = [insight]) {
  return [
    ...workspaceRoutes(),
    { path: '/api/businesses/b1/tracked-sources', body: paged(sources) },
    { path: '/api/businesses/b1/competitor-insights', body: insights },
  ]
}

describe('competitors page', () => {
  it('lists the tracked accounts with their insights', async () => {
    signIn('Owner')
    fakeApi(routes())
    renderAt(<AppRoutes />, '/competitors')

    const card = (await screen.findByRole('heading', { name: 'Rival Cafe' })).closest('li')!
    expect(within(card).getByText('منافس')).toBeInTheDocument()
    expect(within(card).getByText(/12 منشور/)).toBeInTheDocument()
    expect(await screen.findByText('They post at 7 PM and get the most likes.')).toBeInTheDocument()
    expect(screen.getByText('Move your evening post to 7 PM.')).toBeInTheDocument()
  })

  it('shows an empty state before any account is tracked', async () => {
    signIn('Owner')
    fakeApi(routes([], []))
    renderAt(<AppRoutes />, '/competitors')

    expect(await screen.findByText('ما في حسابات للمتابعة لسا')).toBeInTheDocument()
    expect(await screen.findByText('ما في ملاحظات لسا')).toBeInTheDocument()
  })

  it('validates the link and then adds an account', async () => {
    signIn('Owner')
    const { calls } = fakeApi([
      ...routes([], []),
      { method: 'POST', path: '/api/businesses/b1/tracked-sources', status: 201, body: source },
    ])
    renderAt(<AppRoutes />, '/competitors')

    await userEvent.click((await screen.findAllByRole('button', { name: 'تتبّع حساب جديد' }))[0]!)
    const dialog = await screen.findByRole('dialog')
    await userEvent.type(within(dialog).getByLabelText('رابط الحساب'), 'rival_cafe')
    await userEvent.click(within(dialog).getByRole('button', { name: 'أضف الحساب' }))
    expect(await within(dialog).findByText('اكتب رابطاً صحيحاً يبدأ بـ https://')).toBeInTheDocument()

    await userEvent.clear(within(dialog).getByLabelText('رابط الحساب'))
    await userEvent.type(within(dialog).getByLabelText('رابط الحساب'), 'https://www.instagram.com/rival_cafe')
    await userEvent.selectOptions(within(dialog).getByLabelText('نوع الحساب'), 'Inspiration')
    await userEvent.click(within(dialog).getByRole('button', { name: 'أضف الحساب' }))

    await waitFor(() => expect(calls.find((c) => c.method === 'POST')?.body).toEqual({
      url: 'https://www.instagram.com/rival_cafe', kind: 'Inspiration', displayName: null, notes: null,
    }))
    expect(await screen.findByText('تمت إضافة الحساب')).toBeInTheDocument()
  })

  it('scans an account and pauses it', async () => {
    signIn('Owner')
    const { calls } = fakeApi([
      ...routes(),
      { method: 'POST', path: '/api/businesses/b1/tracked-sources/ts1/scan', body: { trackedSourceId: 'ts1', fetched: 5, added: 3, updated: 2, status: { code: 'Active' } } },
      { method: 'PATCH', path: '/api/businesses/b1/tracked-sources/ts1/status', body: { ...source, status: { id: 2, code: 'Paused', text: 'متوقف' } } },
    ])
    renderAt(<AppRoutes />, '/competitors')

    await userEvent.click(await screen.findByRole('button', { name: 'افحص هلأ' }))
    expect(await screen.findByText('تم الفحص: 3 منشور جديد و2 محدّث')).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'إيقاف' }))
    await waitFor(() => expect(calls.find((c) => c.method === 'PATCH')?.body).toEqual({ status: 'Paused' }))
    expect(await screen.findByText('تم إيقاف المتابعة')).toBeInTheDocument()
  })

  it('shows the scan error the API reports', async () => {
    signIn('Owner')
    fakeApi([
      ...routes(),
      { method: 'POST', path: '/api/businesses/b1/tracked-sources/ts1/scan', body: { trackedSourceId: 'ts1', error: 'Instagram refused the request' } },
    ])
    renderAt(<AppRoutes />, '/competitors')

    await userEvent.click(await screen.findByRole('button', { name: 'افحص هلأ' }))
    expect(await screen.findByText('Instagram refused the request')).toBeInTheDocument()
  })

  it('opens the posts of an account', async () => {
    signIn('Owner')
    fakeApi([...routes(), { path: '/api/businesses/b1/tracked-sources/ts1/posts', body: paged([post]) }])
    renderAt(<AppRoutes />, '/competitors')

    await userEvent.click(await screen.findByRole('button', { name: 'شوف المنشورات' }))
    const dialog = await screen.findByRole('dialog', { name: 'منشورات Rival Cafe' })
    expect(await within(dialog).findByText('Try our new latte')).toBeInTheDocument()
    expect(within(dialog).getByText('ريل')).toBeInTheDocument()
    expect(within(dialog).getByText('سؤال')).toBeInTheDocument()
  })

  it('explains when an account cannot be deleted', async () => {
    signIn('Owner')
    fakeApi([
      ...routes(),
      { method: 'DELETE', path: '/api/businesses/b1/tracked-sources/ts1', status: 409, body: { title: 'Conflict', detail: 'Recommendations depend on this source.' } },
    ])
    renderAt(<AppRoutes />, '/competitors')

    await userEvent.click(await screen.findByRole('button', { name: 'حذف Rival Cafe' }))
    const dialog = await screen.findByRole('dialog')
    await userEvent.click(within(dialog).getByRole('button', { name: 'حذف' }))
    expect(await screen.findByText('Recommendations depend on this source.')).toBeInTheDocument()
  })

  it('asks for a new analysis', async () => {
    signIn('Owner')
    const { calls } = fakeApi([
      ...routes(),
      { method: 'POST', path: '/api/businesses/b1/competitor-insights/analyze', body: { sources: 1, postsAnalyzed: 12, insights: [insight] } },
    ])
    renderAt(<AppRoutes />, '/competitors')

    await userEvent.click(await screen.findByRole('button', { name: 'حلّل المنافسين' }))
    expect(await screen.findByText('طلع 1 ملاحظة من 12 منشور')).toBeInTheDocument()
    expect(calls.some((c) => c.method === 'POST' && c.url.endsWith('/analyze'))).toBe(true)
  })

  it('lets a viewer read but not change anything', async () => {
    signIn('Viewer')
    fakeApi(routes())
    renderAt(<AppRoutes />, '/competitors')

    expect(await screen.findByRole('heading', { name: 'Rival Cafe' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'تتبّع حساب جديد' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'افحص هلأ' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'حلّل المنافسين' })).not.toBeInTheDocument()
  })

  it('passes the kind filter to the API', async () => {
    signIn('Owner')
    const { calls } = fakeApi(routes())
    renderAt(<AppRoutes />, '/competitors')

    await userEvent.click(await screen.findByRole('button', { name: 'مصدر إلهام' }))
    await waitFor(() => expect(calls.some((c) => c.url.includes('/tracked-sources?') && c.url.includes('Kind=Inspiration'))).toBe(true))
  })
})
