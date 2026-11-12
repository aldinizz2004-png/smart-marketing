import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { AppRoutes } from '@/app/routes'
import { business, paged, plans, platforms, tenantWithAccess, tenantWithoutAccess } from '@/test/fixtures'
import { fakeApi, renderAt, signIn } from '@/test/render'

const pending = {
  id: 's9', plan: 'Pro', duration: 'OneMonth', price: 45, currency: 'USD', maxPostsPerDay: 10,
  startsAt: '2026-11-06T00:00:00Z', endsAt: '2026-12-06T00:00:00Z', status: { id: 301, code: 'Pending', text: 'بانتظار الدفع' }, grantsAccessNow: false,
}

describe('onboarding', () => {
  it('sends a new owner from the app to the plan step', async () => {
    signIn('Owner')
    fakeApi([
      { path: '/api/tenant', body: tenantWithoutAccess },
      { path: '/api/plans', body: plans },
      { path: '/api/tenant/subscriptions', body: paged([]) },
    ])
    renderAt(<AppRoutes />, '/')

    expect(await screen.findByRole('heading', { name: 'اختر باقتك' })).toBeInTheDocument()
    expect(await screen.findAllByRole('button', { name: 'اختر الباقة' })).toHaveLength(3)
  })

  it('chooses a plan, pays, then creates the shop', async () => {
    signIn('Owner')
    let state: 'none' | 'pending' | 'active' = 'none'
    let created = false
    const { calls } = fakeApi([
      { path: '/api/tenant', body: () => (state === 'active' ? tenantWithAccess : tenantWithoutAccess) },
      { path: '/api/plans', body: plans },
      { path: '/api/platforms', body: platforms },
      { path: '/api/tenant/subscriptions', body: () => paged(state === 'pending' ? [pending] : []) },
      { method: 'POST', path: '/api/tenant/subscriptions', status: 201, body: () => ((state = 'pending'), pending) },
      { method: 'POST', path: '/api/tenant/subscriptions/s9/activate', body: () => ((state = 'active'), { ...pending, grantsAccessNow: true }) },
      { path: '/api/businesses', body: () => paged(created ? [business] : []) },
      { method: 'POST', path: '/api/businesses', status: 201, body: () => ((created = true), business) },
    ])
    renderAt(<AppRoutes />, '/onboarding')

    const choose = await screen.findAllByRole('button', { name: 'اختر الباقة' })
    await userEvent.click(choose[1]!)
    expect(await screen.findByRole('heading', { name: 'خطوة أخيرة: الدفع' })).toBeInTheDocument()

    await userEvent.type(screen.getByLabelText('رقم الحوالة أو المرجع'), 'TRX-123')
    await userEvent.click(screen.getByRole('button', { name: 'تفعيل الاشتراك' }))

    expect(await screen.findByRole('heading', { name: 'عرّفنا على محلك' })).toBeInTheDocument()
    await userEvent.type(screen.getByLabelText('اسم المحل'), 'Qahwet Al-Hara')
    await userEvent.selectOptions(screen.getByLabelText('نوع النشاط'), 'Cafe')
    await userEvent.click(screen.getByRole('button', { name: 'حفظ والمتابعة' }))

    expect(await screen.findByRole('heading', { name: 'كيف يتكلم محلك؟' })).toBeInTheDocument()
    expect(calls.find((c) => c.method === 'POST' && c.url === '/api/tenant/subscriptions')!.body).toEqual({ plan: 'Pro', duration: 'OneMonth' })
    expect(calls.find((c) => c.url.endsWith('/activate'))!.body).toEqual({ paymentReference: 'TRX-123' })
    expect(calls.find((c) => c.method === 'POST' && c.url === '/api/businesses')!.body).toMatchObject({
      name: 'Qahwet Al-Hara', category: 'Cafe', countryCode: 'PS', defaultCurrency: 'ILS',
    })
  })

  it('tells a team member to ask the owner instead of showing prices', async () => {
    signIn('Editor')
    fakeApi([{ path: '/api/tenant', body: tenantWithoutAccess }])
    renderAt(<AppRoutes />, '/onboarding')

    expect(await screen.findByText('مالك الحساب فقط يستطيع تفعيل الاشتراك. اطلب منه إكماله.')).toBeInTheDocument()
  })
})
