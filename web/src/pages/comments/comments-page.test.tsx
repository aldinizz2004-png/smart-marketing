import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { AppRoutes } from '@/app/routes'
import { paged, workspaceRoutes } from '@/test/fixtures'
import { fakeApi, renderAt, signIn } from '@/test/render'

const comment = {
  id: 'cm1',
  contentItemId: 'c1',
  platform: 'instagram',
  authorName: 'Lina',
  authorUsername: 'lina.k',
  body: 'شو أسعار اللاتيه؟',
  postedAt: '2026-01-01T10:00:00Z',
  category: 'Question',
  needsReply: true,
  suggestedReply: 'أهلين لينا، اللاتيه بـ 14 شيكل',
  status: { code: 'Analyzed', text: 'محلَّل' },
}

function routes(items = [comment]) {
  return [
    ...workspaceRoutes(),
    { path: '/api/businesses/b1/social-accounts', body: [] },
    { path: '/api/businesses/b1/comments', body: paged(items) },
  ]
}

describe('comments page', () => {
  it('sends the suggested reply after editing it', async () => {
    signIn('Owner')
    const { calls } = fakeApi([
      ...routes(),
      { method: 'POST', path: '/api/businesses/b1/comments/cm1/reply', body: { ...comment, status: { code: 'Replied' } } },
    ])
    renderAt(<AppRoutes />, '/comments')

    const box = await screen.findByLabelText('الرد المقترح (بتقدر تعدّله)')
    expect(box).toHaveValue('أهلين لينا، اللاتيه بـ 14 شيكل')
    expect(box.closest('li')).toHaveTextContent('سؤال')

    await userEvent.type(box, ' ☕')
    await userEvent.click(screen.getByRole('button', { name: 'أرسل الرد' }))

    await waitFor(() => expect(calls.find((c) => c.method === 'POST')?.body).toEqual({ text: 'أهلين لينا، اللاتيه بـ 14 شيكل ☕' }))
    expect(await screen.findByText('تم إرسال الرد')).toBeInTheDocument()
  })

  it('passes the filters to the API', async () => {
    signIn('Owner')
    const { calls } = fakeApi(routes())
    renderAt(<AppRoutes />, '/comments')

    await userEvent.click(await screen.findByRole('button', { name: 'شكوى' }))
    await userEvent.click(screen.getByRole('button', { name: 'اللي بحاجة رد بس' }))

    await waitFor(() =>
      expect(calls.some((c) => c.url.includes('/comments?') && c.url.includes('Category=Complaint') && c.url.includes('NeedsReply=true'))).toBe(true),
    )
  })

  it('lets an editor ignore but not reply', async () => {
    signIn('Editor')
    fakeApi(routes())
    renderAt(<AppRoutes />, '/comments')

    expect(await screen.findByRole('button', { name: 'تجاهل' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'أرسل الرد' })).not.toBeInTheDocument()
  })

  it('hides sync and analyze from a reviewer', async () => {
    signIn('Reviewer')
    fakeApi(routes())
    renderAt(<AppRoutes />, '/comments')

    expect(await screen.findByRole('button', { name: 'أرسل الرد' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'جلب التعليقات' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'تجاهل' })).not.toBeInTheDocument()
  })
})

describe('comment card', () => {
  it('fills the reply box when the analysis brings a suggestion', async () => {
    signIn('Owner')
    let analyzed = false
    const fresh = { ...comment, category: null, needsReply: null, suggestedReply: null, status: { code: 'New', text: 'جديد' } }
    fakeApi([
      ...workspaceRoutes(),
      { path: '/api/businesses/b1/social-accounts', body: [] },
      { path: '/api/businesses/b1/comments', body: (c: { url: string }) => (c.url.includes('Status=New') ? { ...paged(analyzed ? [] : [fresh]), totalCount: analyzed ? 0 : 1 } : paged([analyzed ? comment : fresh])) },
      { method: 'POST', path: '/api/businesses/b1/comments/analyze', body: () => { analyzed = true; return { analyzed: 1, needReply: 1 } } },
    ])
    renderAt(<AppRoutes />, '/comments')

    expect(await screen.findByLabelText('ردّك')).toHaveValue('')
    await userEvent.click(await screen.findByRole('button', { name: 'حلّل الجديد (1)' }))
    expect(await screen.findByLabelText('الرد المقترح (بتقدر تعدّله)')).toHaveValue('أهلين لينا، اللاتيه بـ 14 شيكل')
  })
})
