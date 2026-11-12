import { zodResolver } from '@hookform/resolvers/zod'
import { CircleCheck, Send } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import { buttonVariants } from '@/components/ui/button-variants'
import { Field } from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { Spinner } from '@/components/ui/spinner'
import { Textarea } from '@/components/ui/textarea'
import { businessCategories } from '@/lib/options'

const schema = z.object({
  fullName: z.string().trim().min(2, 'validation.required').max(100, 'validation.tooLong'),
  shopName: z.string().trim().min(2, 'validation.required').max(150, 'validation.tooLong'),
  email: z.string().trim().min(1, 'validation.required').email('validation.email'),
  phone: z.string().trim().regex(/^$|^\+?[0-9 -]{6,30}$/, 'validation.phone'),
  category: z.string().min(1, 'validation.required'),
  message: z.string().trim().max(500, 'validation.tooLong'),
  consent: z.boolean().refine((v) => v, 'landing.demo.consentRequired'),
})

type Values = z.infer<typeof schema>

const storageKey = 'smp.demoRequests'

function saveRequest(values: Values) {
  try {
    const saved = JSON.parse(localStorage.getItem(storageKey) ?? '[]') as unknown[]
    localStorage.setItem(storageKey, JSON.stringify([...saved, { ...values, sentAt: new Date().toISOString() }]))
  } catch {
    // storage blocked: the confirmation is still shown
  }
}

export function DemoRequestSection() {
  const { t } = useTranslation()
  const [sentTo, setSentTo] = useState<string | null>(null)
  const { register, handleSubmit, formState, reset } = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: { fullName: '', shopName: '', email: '', phone: '', category: '', message: '', consent: false },
  })
  const { errors, isSubmitting } = formState

  const submit = handleSubmit(async (values) => {
    await new Promise((resolve) => setTimeout(resolve, 400))
    saveRequest(values)
    setSentTo(values.fullName)
    reset()
  })

  return (
    <section id="demo" aria-labelledby="demo-title" className="scroll-mt-20 py-20">
      <div className="mx-auto grid max-w-6xl gap-10 px-4 sm:px-6 lg:grid-cols-[1fr_1.2fr] lg:items-start">
        <header>
          <p className="font-semibold text-brand-600 dark:text-brand-400">{t('landing.demo.eyebrow')}</p>
          <h2 id="demo-title" className="mt-2 text-3xl font-extrabold text-balance sm:text-4xl">{t('landing.demo.title')}</h2>
          <p className="mt-4 leading-relaxed text-muted">{t('landing.demo.subtitle')}</p>
          <ul className="mt-6 space-y-3">
            {(['setup', 'walkthrough', 'questions'] as const).map((key) => (
              <li key={key} className="flex items-start gap-3">
                <CircleCheck className="mt-0.5 size-5 shrink-0 text-brand-500" aria-hidden />
                {t(`landing.demo.points.${key}`)}
              </li>
            ))}
          </ul>
        </header>

        <div className="rounded-3xl border border-line bg-surface p-6 shadow-xl shadow-brand-900/5 sm:p-8">
          {sentTo ? (
            <div role="status" className="py-6 text-center">
              <CircleCheck className="mx-auto size-14 text-brand-500" aria-hidden />
              <h3 className="mt-4 text-xl font-bold">{t('landing.demo.thanks', { name: sentTo })}</h3>
              <p className="mt-2 text-muted">{t('landing.demo.thanksHint')}</p>
              <div className="mt-6 flex flex-wrap justify-center gap-3">
                <Link to="/register" className={buttonVariants()}>{t('landing.nav.start')}</Link>
                <Button variant="secondary" onClick={() => setSentTo(null)}>{t('landing.demo.another')}</Button>
              </div>
            </div>
          ) : (
            <form onSubmit={(e) => void submit(e)} noValidate aria-labelledby="demo-title" className="grid gap-4 sm:grid-cols-2">
              <Field label={t('landing.demo.fullName')} error={errors.fullName?.message}>
                <Input autoComplete="name" {...register('fullName')} />
              </Field>
              <Field label={t('landing.demo.shopName')} error={errors.shopName?.message}>
                <Input autoComplete="organization" {...register('shopName')} />
              </Field>
              <Field label={t('landing.demo.email')} error={errors.email?.message}>
                <Input type="email" dir="ltr" autoComplete="email" {...register('email')} />
              </Field>
              <Field label={t('landing.demo.phone')} optional error={errors.phone?.message}>
                <Input type="tel" dir="ltr" autoComplete="tel" {...register('phone')} />
              </Field>
              <div className="sm:col-span-2">
                <Field label={t('landing.demo.category')} error={errors.category?.message}>
                  <Select {...register('category')}>
                    <option value="">{t('landing.demo.pickCategory')}</option>
                    {businessCategories.map((c) => <option key={c} value={c}>{t(`business.categories.${c}`)}</option>)}
                    <option value="other">{t('business.categories.other')}</option>
                  </Select>
                </Field>
              </div>
              <div className="sm:col-span-2">
                <Field label={t('landing.demo.message')} optional hint={t('landing.demo.messageHint')} error={errors.message?.message}>
                  <Textarea rows={3} maxLength={500} {...register('message')} />
                </Field>
              </div>
              <div className="sm:col-span-2">
                <label className="flex items-start gap-3 text-sm">
                  <input
                    type="checkbox"
                    className="mt-0.5 size-4 shrink-0 accent-brand-600"
                    aria-invalid={errors.consent ? true : undefined}
                    aria-describedby={errors.consent ? 'demo-consent-error' : undefined}
                    {...register('consent')}
                  />
                  {t('landing.demo.consent')}
                </label>
                {errors.consent?.message && (
                  <p id="demo-consent-error" className="mt-1.5 text-sm text-coral-500">{t(errors.consent.message)}</p>
                )}
              </div>
              <div className="sm:col-span-2">
                <Button type="submit" size="lg" className="w-full" disabled={isSubmitting}>
                  {isSubmitting ? <Spinner /> : <Send className="rtl:-scale-x-100" aria-hidden />}
                  {t('landing.demo.submit')}
                </Button>
              </div>
            </form>
          )}
        </div>
      </div>
    </section>
  )
}
