import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { AppRoutes } from '@/app/routes'
import { paged, workspaceRoutes } from '@/test/fixtures'
import { fakeApi, renderAt, signIn } from '@/test/render'

const latte = { id: 'p1', name: 'لاتيه الفستق', price: 14, currency: 'ILS', isActive: true }
const offer = (code: string) => ({
  id: 'o1',
  title: 'خصم الويكند',
  discountPercent: 20,
  startsAt: '2030-01-01T10:00:00Z',
  endsAt: '2030-01-03T10:00:00Z',
  status: { code, text: code },
  isRunningNow: code === 'Active',
})

describe('catalog page', () => {
  it('adds a product', async () => {
    signIn('Owner')
    const { calls } = fakeApi([
      ...workspaceRoutes(),
      { path: '/api/businesses/b1/products', body: paged([latte]) },
      { method: 'POST', path: '/api/businesses/b1/products', body: { ...latte, id: 'p2' } },
    ])
    renderAt(<AppRoutes />, '/catalog')

    expect(await screen.findByText('لاتيه الفستق')).toBeInTheDocument()
    expect(screen.getByText(/₪14/)).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'منتج جديد' }))
    const dialog = screen.getByRole('dialog')
    await userEvent.type(within(dialog).getByLabelText('اسم المنتج'), 'كنافة')
    await userEvent.type(within(dialog).getByLabelText('السعر'), '12.5')
    await userEvent.click(within(dialog).getByRole('button', { name: 'حفظ' }))

    await waitFor(() =>
      expect(calls.find((c) => c.method === 'POST')?.body).toEqual({ name: 'كنافة', sku: null, description: null, price: 12.5, currency: 'ILS', isActive: true }),
    )
  })

  it('creates a percent-off offer', async () => {
    signIn('Owner')
    const { calls } = fakeApi([
      ...workspaceRoutes(),
      { path: '/api/businesses/b1/offers', body: paged([]) },
      { method: 'POST', path: '/api/businesses/b1/offers', body: offer('Draft') },
    ])
    renderAt(<AppRoutes />, '/catalog?tab=offers')

    await userEvent.click(await screen.findByRole('button', { name: 'عرض جديد' }))
    const dialog = screen.getByRole('dialog')
    await userEvent.type(within(dialog).getByLabelText('عنوان العرض'), 'خصم الويكند')
    await userEvent.click(within(dialog).getByRole('radio', { name: 'نسبة خصم' }))
    await userEvent.type(within(dialog).getByLabelText('نسبة الخصم %'), '150')
    await userEvent.click(within(dialog).getByRole('button', { name: 'حفظ' }))
    expect(await within(dialog).findByText('النسبة بين 0 و 100')).toBeInTheDocument()

    await userEvent.clear(within(dialog).getByLabelText('نسبة الخصم %'))
    await userEvent.type(within(dialog).getByLabelText('نسبة الخصم %'), '20')
    await userEvent.click(within(dialog).getByRole('button', { name: 'حفظ' }))
    await waitFor(() => expect(calls.find((c) => c.method === 'POST')?.body).toMatchObject({ title: 'خصم الويكند', discountPercent: 20, offerPrice: null, currency: null }))
  })

  it('offers only the status moves the API allows', async () => {
    signIn('Owner')
    const { calls } = fakeApi([
      ...workspaceRoutes(),
      { path: '/api/businesses/b1/offers', body: paged([offer('Active')]) },
      { method: 'PATCH', path: '/api/businesses/b1/offers/o1/status', body: offer('Paused') },
    ])
    renderAt(<AppRoutes />, '/catalog?tab=offers')

    expect(await screen.findByText('خصم 20%')).toBeInTheDocument()
    expect(screen.getByText('شغّال هلأ')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'تفعيل' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /حذف/ })).not.toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'إيقاف مؤقت' }))
    await waitFor(() => expect(calls.find((c) => c.method === 'PATCH')?.body).toEqual({ status: 'Paused' }))
  })

  it('stops a file that does not fit the chosen type', async () => {
    signIn('Owner')
    fakeApi([
      ...workspaceRoutes(),
      { path: '/api/businesses/b1/media', body: paged([]) },
      { path: '/api/businesses/b1/products', body: paged([]) },
    ])
    renderAt(<AppRoutes />, '/catalog?tab=media')

    await userEvent.click(await screen.findByRole('button', { name: 'رفع ملفات' }))
    const file = new File(['%PDF'], 'menu.pdf', { type: 'application/pdf' })
    fireEvent.change(screen.getByTestId('media-file'), { target: { files: [file] } })
    expect(await screen.findByRole('alert')).toHaveTextContent('menu.pdf')
    expect(screen.getByRole('button', { name: 'ارفع (0)' })).toBeDisabled()
  })

  it('is read-only for a viewer', async () => {
    signIn('Viewer')
    fakeApi([...workspaceRoutes(), { path: '/api/businesses/b1/products', body: paged([latte]) }])
    renderAt(<AppRoutes />, '/catalog')

    expect(await screen.findByText('لاتيه الفستق')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'منتج جديد' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'إخفاء' })).not.toBeInTheDocument()
  })
})
