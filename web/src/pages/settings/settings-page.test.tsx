import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { AppRoutes } from '@/app/routes'
import { activeSubscription, paged, workspaceRoutes } from '@/test/fixtures'
import { fakeApi, renderAt, signIn } from '@/test/render'

const b = '/api/businesses/b1'

const policy = {
  businessId: 'b1',
  isConfigured: true,
  automationEnabled: false,
  maxPostsPerDay: 3,
  plan: 'Pro',
  planMaxPostsPerDay: 10,
  publishWindowStart: '09:00:00',
  publishWindowEnd: '21:00:00',
  maxDiscountPercent: 20,
  monthlyAdBudget: null,
  budgetCurrency: null,
  requireSeparateReviewer: false,
  autoReplyToMessages: false,
  allowedPlatforms: ['instagram'],
  autoApplyActions: [],
  updatedAt: '2026-11-10T10:00:00Z',
}

const members = [
  { userId: 'u1', email: 'sara@test.local', fullName: 'Sara Haddad', role: 'Owner', status: { id: 1, code: 'Active', text: 'Active' }, joinedAt: '2026-10-01T10:00:00Z' },
  { userId: 'u2', email: 'lina@test.local', fullName: 'Lina Nasser', role: 'Editor', status: { id: 1, code: 'Active', text: 'Active' }, joinedAt: '2026-10-05T10:00:00Z' },
]

const usage = {
  totalCostUsd: 1.25,
  executions: { total: 40, succeeded: 38, failed: 2, averageLatencyMs: 900 },
  byEventType: [{ eventType: 'CopyGeneration', events: 30, units: 30, costUsd: 0.75 }],
  byBusiness: [],
}

function routes(extra: Parameters<typeof fakeApi>[0] = []) {
  return [
    ...extra,
    ...workspaceRoutes(),
    { path: `${b}/brand-profile`, status: 404, body: { title: 'Not Found' } },
    { path: `${b}/brand-terms`, body: [] },
    { path: `${b}/social-accounts`, body: [] },
    { path: `${b}/automation-policy`, body: policy },
    { path: '/api/tenant/members', body: paged(members) },
    { path: '/api/tenant/subscriptions', body: paged([activeSubscription]) },
    { path: '/api/tenant/ai-usage', body: usage },
  ]
}

describe('settings page', () => {
  it('saves the shop details', async () => {
    signIn('Owner')
    const { calls } = fakeApi(routes([{ method: 'PUT', path: '/api/businesses/b1', body: { id: 'b1', name: 'New Name' } }]))
    renderAt(<AppRoutes />, '/settings')

    const name = await screen.findByLabelText('اسم المحل')
    await userEvent.clear(name)
    await userEvent.type(name, 'New Name')
    await userEvent.click(screen.getAllByRole('button', { name: 'حفظ' })[0]!)

    await waitFor(() => expect(calls.find((c) => c.method === 'PUT')?.body).toMatchObject({ name: 'New Name', category: 'Cafe' }))
    expect(await screen.findByText('تم الحفظ')).toBeInTheDocument()
  })

  it('shows the details read-only to someone who cannot edit', async () => {
    signIn('Viewer')
    fakeApi(routes())
    renderAt(<AppRoutes />, '/settings')

    expect(await screen.findByText('تعديل بيانات المحل للمدير أو المالك فقط.')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'حفظ' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'أضف الكلمة' })).not.toBeInTheDocument()
  })

  it('adds a prohibited word to the brand', async () => {
    signIn('Owner')
    const { calls } = fakeApi(routes([{ method: 'POST', path: `${b}/brand-terms`, status: 201, body: { id: 't1', term: 'cheap', rule: 'Prohibited' } }]))
    renderAt(<AppRoutes />, '/settings')

    await userEvent.type(await screen.findByLabelText('الكلمة أو العبارة'), 'cheap')
    await userEvent.selectOptions(screen.getByLabelText('القاعدة'), 'Prohibited')
    await userEvent.click(screen.getByRole('button', { name: 'أضف الكلمة' }))

    await waitFor(() => expect(calls.find((c) => c.method === 'POST')?.body).toEqual({ term: 'cheap', rule: 'Prohibited', note: null }))
    expect(await screen.findByText('تمت إضافة الكلمة')).toBeInTheDocument()
  })

  it('saves the automation policy', async () => {
    signIn('Owner')
    const { calls } = fakeApi(routes([{ method: 'PUT', path: `${b}/automation-policy`, body: { ...policy, automationEnabled: true, maxPostsPerDay: 5 } }]))
    renderAt(<AppRoutes />, '/settings?tab=automation')

    await userEvent.click(await screen.findByLabelText('تشغيل الأتمتة'))
    const perDay = screen.getByLabelText('أقصى عدد منشورات يومياً')
    await userEvent.clear(perDay)
    await userEvent.type(perDay, '5')
    await userEvent.click(screen.getByLabelText('منشور جديد'))
    await userEvent.click(screen.getByRole('button', { name: 'حفظ' }))

    await waitFor(() =>
      expect(calls.find((c) => c.method === 'PUT')?.body).toEqual({
        automationEnabled: true,
        maxPostsPerDay: 5,
        publishWindowStart: '09:00:00',
        publishWindowEnd: '21:00:00',
        maxDiscountPercent: 20,
        monthlyAdBudget: null,
        budgetCurrency: null,
        requireSeparateReviewer: false,
        autoReplyToMessages: false,
        allowedPlatforms: ['instagram'],
        autoApplyActions: ['NewContent'],
      }),
    )
    expect(await screen.findByText('تم الحفظ')).toBeInTheDocument()
  })

  it('does not send more posts per day than the plan allows', async () => {
    signIn('Owner')
    const { calls } = fakeApi(routes())
    renderAt(<AppRoutes />, '/settings?tab=automation')

    const perDay = await screen.findByLabelText('أقصى عدد منشورات يومياً')
    await userEvent.clear(perDay)
    await userEvent.type(perDay, '11')
    await userEvent.click(screen.getByRole('button', { name: 'حفظ' }))

    expect(await screen.findByText('باقتك تسمح بـ 10 منشورات يومياً كحد أقصى.')).toBeInTheDocument()
    expect(calls.some((c) => c.method === 'PUT')).toBe(false)
  })

  it('lists the team and changes a role', async () => {
    signIn('Owner')
    const { calls } = fakeApi(routes([{ method: 'PATCH', path: '/api/tenant/members/u2', body: { ...members[1], role: 'Reviewer' } }]))
    renderAt(<AppRoutes />, '/settings?tab=team')

    expect(await screen.findByText('Lina Nasser')).toBeInTheDocument()
    expect(screen.getByLabelText('دور Lina Nasser')).toBeInTheDocument()
    expect(screen.queryByLabelText('دور Sara Haddad')).not.toBeInTheDocument()

    await userEvent.selectOptions(screen.getByLabelText('دور Lina Nasser'), 'Reviewer')
    await waitFor(() => expect(calls.find((c) => c.method === 'PATCH')?.body).toEqual({ role: 'Reviewer' }))
    expect(await screen.findByText('تم تغيير الدور')).toBeInTheDocument()
  })

  it('validates the email and then adds a member', async () => {
    signIn('Owner')
    const { calls } = fakeApi(routes([{ method: 'POST', path: '/api/tenant/members', status: 201, body: members[1] }]))
    renderAt(<AppRoutes />, '/settings?tab=team')

    await userEvent.click(await screen.findByRole('button', { name: 'إضافة عضو' }))
    const dialog = await screen.findByRole('dialog')
    await userEvent.type(within(dialog).getByLabelText('البريد الإلكتروني'), 'not-an-email')
    await userEvent.click(within(dialog).getByRole('button', { name: 'إضافة عضو' }))
    expect(await within(dialog).findByText('اكتب بريداً إلكترونياً صحيحاً')).toBeInTheDocument()

    await userEvent.clear(within(dialog).getByLabelText('البريد الإلكتروني'))
    await userEvent.type(within(dialog).getByLabelText('البريد الإلكتروني'), 'new@test.local')
    await userEvent.selectOptions(within(dialog).getByLabelText('الدور'), 'Reviewer')
    await userEvent.click(within(dialog).getByRole('button', { name: 'إضافة عضو' }))

    await waitFor(() => expect(calls.find((c) => c.method === 'POST')?.body).toEqual({ email: 'new@test.local', role: 'Reviewer' }))
    expect(await screen.findByText('تمت إضافة العضو')).toBeInTheDocument()
  })

  it('keeps the team read-only for an editor', async () => {
    signIn('Editor')
    fakeApi(routes())
    renderAt(<AppRoutes />, '/settings?tab=team')

    expect(await screen.findByText('Lina Nasser')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'إضافة عضو' })).not.toBeInTheDocument()
    expect(screen.queryByLabelText('دور Lina Nasser')).not.toBeInTheDocument()
  })

  it('shows the subscription and the AI usage', async () => {
    signIn('Owner')
    fakeApi(routes())
    renderAt(<AppRoutes />, '/settings?tab=plan')

    expect(await screen.findByText('الاحترافية', { selector: 'p' })).toBeInTheDocument()
    expect(await screen.findByText('كتابة نصوص')).toBeInTheDocument()
    expect(screen.getByText('30 مرة')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'إلغاء الاشتراك' })).toBeInTheDocument()
  })

  it('cancels the subscription after confirming', async () => {
    signIn('Owner')
    const { calls } = fakeApi(routes([{ method: 'POST', path: '/api/tenant/subscriptions/s1/cancel', body: { ...activeSubscription, status: { code: 'Canceled' } } }]))
    renderAt(<AppRoutes />, '/settings?tab=plan')

    await userEvent.click(await screen.findByRole('button', { name: 'إلغاء الاشتراك' }))
    const dialog = await screen.findByRole('dialog', { name: 'إلغاء الاشتراك؟' })
    await userEvent.click(within(dialog).getByRole('button', { name: 'إلغاء الاشتراك' }))

    await waitFor(() => expect(calls.some((c) => c.method === 'POST' && c.url.endsWith('/s1/cancel'))).toBe(true))
    expect(await screen.findByText('تم إلغاء الاشتراك')).toBeInTheDocument()
  })

  it('lists connected accounts and disconnects one', async () => {
    signIn('Owner')
    const account = { id: 'a1', platformCode: 'instagram', handle: 'qahwet', status: { id: 1, code: 'Connected', text: 'Connected' } }
    const { calls } = fakeApi(routes([
      { path: `${b}/social-accounts`, body: [account] },
      { method: 'POST', path: `${b}/social-accounts/a1/disconnect`, body: { ...account, status: { code: 'Disconnected' } } },
    ]))
    renderAt(<AppRoutes />, '/settings?tab=accounts')

    expect(await screen.findByText('@qahwet')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'فصل' }))
    const dialog = await screen.findByRole('dialog')
    await userEvent.click(within(dialog).getByRole('button', { name: 'فصل' }))

    await waitFor(() => expect(calls.some((c) => c.url.endsWith('/a1/disconnect'))).toBe(true))
    expect(await screen.findByText('تم فصل الحساب')).toBeInTheDocument()
  })
})
