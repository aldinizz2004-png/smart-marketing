import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { AppRoutes } from '@/app/routes'
import { paged, workspaceRoutes } from '@/test/fixtures'
import { fakeApi, renderAt, signIn } from '@/test/render'

const b = '/api/businesses/b1'

describe('create post page', () => {
  it('asks for a campaign instead of doing nothing when there is none', async () => {
    signIn('Owner')
    const { calls } = fakeApi([
      ...workspaceRoutes(),
      { path: `${b}/campaigns`, body: paged([]) },
      { path: `${b}/social-accounts`, body: [] },
      { path: `${b}/products`, body: paged([]) },
      { path: `${b}/offers`, body: paged([]) },
    ])
    renderAt(<AppRoutes />, '/content/new')

    await userEvent.click(await screen.findByRole('button', { name: 'أنشئ المنشور' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('كل منشور يتبع حملة. أنشئ حملتك الأولى.')
    expect(await screen.findByRole('dialog')).toBeInTheDocument()
    expect(calls.some((c) => c.method === 'POST')).toBe(false)
  })
})
