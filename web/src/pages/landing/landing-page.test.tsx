import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { AppRoutes } from '@/app/routes'
import { renderAt, signIn, signOut } from '@/test/render'

describe('landing page', () => {
  it('greets a first-time visitor at the root', async () => {
    signOut()
    renderAt(<AppRoutes />, '/')

    expect(await screen.findByRole('heading', { level: 1, name: /سوشال ميديا محلك/ })).toBeInTheDocument()
    expect(screen.getAllByRole('link', { name: 'إنشاء حساب' })[0]).toHaveAttribute('href', '/register')
  })

  it('sends a returning visitor to sign in', async () => {
    localStorage.setItem('smp.signedIn', '1')
    signOut()
    renderAt(<AppRoutes />, '/')

    expect(await screen.findByRole('button', { name: 'دخول' })).toBeInTheDocument()
  })

  it('offers the dashboard to someone already signed in', async () => {
    signIn()
    renderAt(<AppRoutes />, '/welcome')

    expect((await screen.findAllByRole('link', { name: 'لوحة التحكم' }))[0]).toHaveAttribute('href', '/')
    expect(screen.queryByRole('link', { name: 'تسجيل الدخول' })).not.toBeInTheDocument()
  })

  it('opens the menu on small screens', async () => {
    signOut()
    renderAt(<AppRoutes />, '/welcome')

    const toggle = await screen.findByRole('button', { name: 'فتح القائمة' })
    expect(toggle).toHaveAttribute('aria-expanded', 'false')
    await userEvent.click(toggle)
    expect(screen.getByRole('button', { name: 'إغلاق القائمة' })).toHaveAttribute('aria-expanded', 'true')
  })
})

describe('landing features', () => {
  it('lists six features in their own section', async () => {
    signOut()
    renderAt(<AppRoutes />, '/welcome')

    const section = await screen.findByRole('region', { name: 'من فكرة المنشور لحد تقرير آخر الأسبوع' })
    expect(within(section).getAllByRole('article')).toHaveLength(6)
    expect(screen.getAllByRole('link', { name: 'المميزات' })[0]).toHaveAttribute('href', '#features')
  })
})

describe('landing tech stack', () => {
  it('shows the technologies as badges', async () => {
    signOut()
    renderAt(<AppRoutes />, '/welcome')

    const section = await screen.findByRole('region', { name: 'تقنيات حديثة وموثوقة' })
    expect(within(section).getByText('ASP.NET Core 10')).toBeInTheDocument()
    expect(within(section).getByText('React 19')).toBeInTheDocument()
    expect(within(section).getByText('SQL Server 2025')).toBeInTheDocument()
  })
})

describe('landing demo request', () => {
  it('checks the fields and confirms a valid request', async () => {
    signOut()
    renderAt(<AppRoutes />, '/welcome')

    const form = await screen.findByRole('form', { name: 'اطلب عرض تجريبي لمحلك' })
    await userEvent.click(within(form).getByRole('button', { name: 'أرسل الطلب' }))
    expect(await within(form).findAllByText('هذا الحقل مطلوب')).toHaveLength(4)
    expect(within(form).getByText('لازم توافق عشان نقدر نتواصل معك')).toBeInTheDocument()

    await userEvent.type(within(form).getByLabelText('اسمك'), 'Sara')
    await userEvent.type(within(form).getByLabelText('اسم المحل'), 'Qahwet Al-Hara')
    await userEvent.type(within(form).getByLabelText('البريد الإلكتروني'), 'sara@')
    await userEvent.selectOptions(within(form).getByLabelText('نوع المحل'), 'Cafe')
    await userEvent.click(within(form).getByRole('checkbox'))
    await userEvent.click(within(form).getByRole('button', { name: 'أرسل الطلب' }))
    expect(await within(form).findByText('اكتب بريداً إلكترونياً صحيحاً')).toBeInTheDocument()

    await userEvent.type(within(form).getByLabelText('البريد الإلكتروني'), 'cafe.ps')
    await userEvent.click(within(form).getByRole('button', { name: 'أرسل الطلب' }))
    expect(await screen.findByRole('status')).toHaveTextContent('شكراً Sara! وصلنا طلبك')
    expect(JSON.parse(localStorage.getItem('smp.demoRequests') ?? '[]')).toHaveLength(1)
  })
})

describe('landing architecture dialog', () => {
  it('opens from the hero and closes with Escape', async () => {
    signOut()
    renderAt(<AppRoutes />, '/welcome')

    const heroButton = (await screen.findAllByRole('button', { name: 'شوف بنية النظام' }))[0]!
    await userEvent.click(heroButton)
    const dialog = screen.getByRole('dialog', { name: 'بنية النظام' })
    expect(within(dialog).getByText('SQL Server')).toBeInTheDocument()
    expect(within(dialog).getByText('WhatsApp Cloud API')).toBeInTheDocument()

    await userEvent.keyboard('{Escape}')
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(heroButton).toHaveFocus()
  })
})
