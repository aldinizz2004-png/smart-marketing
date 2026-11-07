import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { AppRoutes } from '@/app/routes'
import { paged, workspaceRoutes } from '@/test/fixtures'
import { fakeApi, renderAt, signIn } from '@/test/render'

const whatsapp = { id: 'sa1', platformCode: 'whatsapp', status: { code: 'Connected' } }
const summary = {
  id: 'cv1',
  customerPhone: '970599000111',
  customerName: 'Omar',
  status: { code: 'WaitingForOwner', text: 'بانتظار ردك' },
  lastIntent: 'Order',
  handoffReason: 'Asks about delivery',
  hasSuggestion: true,
  lastMessage: 'بدي 2 لاتيه',
  lastMessageAt: '2026-01-01T10:00:00Z',
  canReplyFreely: true,
  replyWindowEndsAt: '2026-01-02T10:00:00Z',
}
const messages = [
  { id: 'm1', direction: 'Inbound', author: 'Customer', body: 'بدي 2 لاتيه', sentAt: '2026-01-01T10:00:00Z' },
]

function routes(conversation = summary) {
  return [
    ...workspaceRoutes(),
    { path: '/api/businesses/b1/social-accounts', body: [whatsapp] },
    { path: '/api/businesses/b1/conversations', body: paged([conversation]) },
    { path: '/api/businesses/b1/conversations/cv1', body: { conversation, suggestedReply: 'أكيد عمر، 28 شيكل', messages } },
  ]
}

describe('inbox page', () => {
  it('opens a conversation and sends the suggested reply', async () => {
    signIn('Owner')
    const { calls } = fakeApi([
      ...routes(),
      {
        method: 'POST',
        path: '/api/businesses/b1/conversations/cv1/messages',
        body: { conversation: { ...summary, status: { code: 'Open' } }, suggestedReply: null, messages },
      },
    ])
    renderAt(<AppRoutes />, '/inbox')

    await userEvent.click(await screen.findByRole('link', { name: /Omar/ }))
    expect(await screen.findByText('Asks about delivery')).toBeInTheDocument()
    const box = screen.getByLabelText('الرد المقترح (بتقدر تعدّله)')
    expect(box).toHaveValue('أكيد عمر، 28 شيكل')

    await userEvent.click(screen.getByRole('button', { name: 'إرسال' }))
    await waitFor(() => expect(calls.find((c) => c.method === 'POST')?.body).toEqual({ text: 'أكيد عمر، 28 شيكل' }))
    expect(await screen.findByText('تم إرسال الرسالة')).toBeInTheDocument()
  })

  it('asks for a template once the reply window has closed', async () => {
    signIn('Owner')
    const closedWindow = { ...summary, canReplyFreely: false, replyWindowEndsAt: '2026-01-02T10:00:00Z' }
    const { calls } = fakeApi([
      ...routes(closedWindow),
      { method: 'POST', path: '/api/businesses/b1/conversations/cv1/messages', body: { conversation: closedWindow, messages } },
    ])
    renderAt(<AppRoutes />, '/inbox/cv1')

    await userEvent.type(await screen.findByLabelText('اسم القالب'), 'order_followup')
    await userEvent.click(screen.getByRole('button', { name: 'أرسل القالب' }))
    await waitFor(() => expect(calls.find((c) => c.method === 'POST')?.body).toEqual({ templateName: 'order_followup', languageCode: 'ar' }))
  })

  it('closes a conversation', async () => {
    signIn('Owner')
    const { calls } = fakeApi([
      ...routes(),
      { method: 'PATCH', path: '/api/businesses/b1/conversations/cv1/status', body: { conversation: { ...summary, status: { code: 'Closed' } }, messages } },
    ])
    renderAt(<AppRoutes />, '/inbox/cv1')

    await userEvent.click(await screen.findByRole('button', { name: 'إغلاق المحادثة' }))
    await waitFor(() => expect(calls.find((c) => c.method === 'PATCH')?.body).toEqual({ status: 'Closed' }))
    expect(await screen.findByText('المحادثة مغلقة. افتحها مرة ثانية إذا بدك ترد.')).toBeInTheDocument()
  })

  it('points to the settings when WhatsApp is not connected', async () => {
    signIn('Owner')
    fakeApi([
      ...workspaceRoutes(),
      { path: '/api/businesses/b1/social-accounts', body: [] },
      { path: '/api/businesses/b1/conversations', body: paged([]) },
    ])
    renderAt(<AppRoutes />, '/inbox')

    expect(await screen.findByRole('link', { name: 'ربط واتساب' })).toHaveAttribute('href', '/settings/accounts')
  })
})
