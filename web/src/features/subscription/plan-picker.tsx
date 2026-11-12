import { Check, Crown, Rocket, Store } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { ErrorState } from '@/components/error-state'
import { Button } from '@/components/ui/button'
import { SectionSpinner, Spinner } from '@/components/ui/spinner'
import { cn } from '@/lib/cn'
import { formatMoney } from '@/lib/format'
import { durations, usePlans, type Duration, type PlanName } from './api'

const looks: Record<PlanName, { icon: typeof Store; tone: string; popular?: boolean }> = {
  Basic: { icon: Store, tone: 'from-brand-500 to-brand-700' },
  Pro: { icon: Rocket, tone: 'from-sun-400 to-coral-400', popular: true },
  Business: { icon: Crown, tone: 'from-grape-400 to-grape-500' },
}

type PlanPickerProps = {
  currentPlan?: PlanName | null
  busyPlan?: PlanName | null
  onChoose: (plan: PlanName, duration: Duration) => void
  disabled?: boolean
}

export function PlanPicker({ currentPlan, busyPlan, onChoose, disabled }: PlanPickerProps) {
  const { t, i18n } = useTranslation()
  const plans = usePlans()
  const [duration, setDuration] = useState<Duration>('OneMonth')

  if (plans.isPending) return <SectionSpinner />
  if (plans.isError) return <ErrorState error={plans.error} onRetry={() => void plans.refetch()} />

  return (
    <div className="space-y-6">
      <div role="radiogroup" aria-label={t('plans.duration')} className="mx-auto flex w-fit gap-1 rounded-2xl bg-surface-2 p-1">
        {durations.map((d) => (
          <button
            key={d}
            type="button"
            role="radio"
            aria-checked={duration === d}
            onClick={() => setDuration(d)}
            className={cn(
              'rounded-xl px-4 py-2 text-sm font-semibold transition-colors',
              duration === d ? 'bg-surface text-ink shadow' : 'text-muted hover:text-ink',
            )}
          >
            {t(`plans.durations.${d}`)}
          </button>
        ))}
      </div>

      <div className="grid gap-4 md:grid-cols-3">
        {plans.data.map((plan) => {
          const name = plan.plan ?? 'Basic'
          const look = looks[name]
          const Icon = look.icon
          const price = (plan.prices as Record<string, number> | undefined)?.[duration] ?? 0
          const monthly = duration === 'OneMonth' ? price : duration === 'ThreeMonths' ? price / 3 : price / 12
          return (
            <div
              key={name}
              className={cn(
                'relative flex flex-col rounded-3xl border bg-surface p-6 shadow-sm',
                look.popular ? 'border-sun-400 shadow-lg shadow-sun-500/15' : 'border-line',
              )}
            >
              {look.popular && (
                <span className="absolute -top-3 start-6 rounded-full bg-sun-400 px-3 py-0.5 text-xs font-bold text-brand-950">
                  {t('plans.popular')}
                </span>
              )}
              <span className={cn('grid size-12 place-items-center rounded-2xl bg-linear-to-br text-white shadow-md', look.tone)}>
                <Icon className="size-6" aria-hidden />
              </span>
              <h3 className="mt-4 text-xl font-bold">{t(`plans.names.${name}`)}</h3>
              <p className="mt-2 text-3xl font-bold">
                {formatMoney(price, plan.currency ?? 'USD', i18n.language)}
              </p>
              <p className="text-sm text-muted">
                {duration === 'OneMonth'
                  ? t('plans.perMonth')
                  : t('plans.monthlyEquivalent', { price: formatMoney(monthly, plan.currency ?? 'USD', i18n.language) })}
              </p>
              <ul className="mt-5 flex-1 space-y-2 text-sm">
                {['postsPerDay', 'ai', 'comments', 'reports'].map((feature) => (
                  <li key={feature} className="flex gap-2">
                    <Check className="mt-0.5 size-4 shrink-0 text-brand-500" aria-hidden />
                    {t(`plans.features.${feature}`, { count: plan.maxPostsPerDay ?? 0 })}
                  </li>
                ))}
              </ul>
              <Button
                className="mt-6 w-full"
                variant={look.popular ? 'sun' : 'primary'}
                disabled={disabled || busyPlan != null}
                onClick={() => onChoose(name, duration)}
              >
                {busyPlan === name && <Spinner />}
                {currentPlan === name ? t('plans.renew') : t('plans.choose')}
              </Button>
            </div>
          )
        })}
      </div>
    </div>
  )
}
