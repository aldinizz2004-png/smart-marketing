import { zodResolver } from '@hookform/resolvers/zod'
import { Landmark } from 'lucide-react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import { Field } from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { Spinner } from '@/components/ui/spinner'
import { formatDate, formatMoney } from '@/lib/format'
import type { Subscription } from './api'

const schema = z.object({ paymentReference: z.string().trim().min(3, 'validation.paymentReference').max(200, 'validation.tooLong') })
type Values = z.infer<typeof schema>

type PaymentFormProps = {
  subscription: Subscription
  onPay: (paymentReference: string) => Promise<unknown>
  onChangePlan?: () => void
  changing?: boolean
}

// There is no card gateway yet: the owner pays by transfer and records the reference to activate.
export function PaymentForm({ subscription, onPay, onChangePlan, changing }: PaymentFormProps) {
  const { t, i18n } = useTranslation()
  const { register, handleSubmit, formState } = useForm<Values>({ resolver: zodResolver(schema), defaultValues: { paymentReference: '' } })

  return (
    <div className="mx-auto max-w-xl space-y-6">
      <div className="rounded-3xl bg-linear-to-br from-brand-700 to-brand-500 p-6 text-white shadow-lg">
        <p className="text-sm text-white/80">{t('plans.yourChoice')}</p>
        <p className="mt-1 text-2xl font-bold">
          {t(`plans.names.${subscription.plan ?? 'Basic'}`)} · {t(`plans.durations.${subscription.duration ?? 'OneMonth'}`)}
        </p>
        <p className="mt-3 text-3xl font-bold">{formatMoney(subscription.price ?? 0, subscription.currency ?? 'USD', i18n.language)}</p>
        <p className="mt-1 text-sm text-white/80">
          {t('plans.period', { from: formatDate(subscription.startsAt, i18n.language), to: formatDate(subscription.endsAt, i18n.language) })}
        </p>
      </div>

      <div className="flex gap-3 rounded-2xl border border-line bg-surface p-4">
        <Landmark className="mt-0.5 size-5 shrink-0 text-brand-500" aria-hidden />
        <div className="text-sm">
          <p className="font-semibold">{t('plans.howToPay')}</p>
          <p className="mt-1 text-muted">{t('plans.howToPayHint')}</p>
        </div>
      </div>

      <form onSubmit={handleSubmit((v) => onPay(v.paymentReference).catch(() => undefined))} className="space-y-4" noValidate>
        <Field label={t('plans.reference')} hint={t('plans.referenceHint')} error={formState.errors.paymentReference?.message}>
          <Input dir="ltr" {...register('paymentReference')} />
        </Field>
        <div className="flex flex-wrap gap-2">
          <Button type="submit" size="lg" disabled={formState.isSubmitting}>
            {formState.isSubmitting && <Spinner />}
            {t('plans.activate')}
          </Button>
          {onChangePlan && (
            <Button type="button" variant="ghost" size="lg" onClick={onChangePlan} disabled={changing}>
              {changing && <Spinner />}
              {t('plans.changePlan')}
            </Button>
          )}
        </div>
      </form>
    </div>
  )
}
