import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { can } from '@/auth/roles'
import { useSession } from '@/auth/use-session'
import { ConfirmDialog } from '@/components/confirm-dialog'
import { ErrorState } from '@/components/error-state'
import { StatusBadge } from '@/components/status-badge'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Field } from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { SectionSpinner, Spinner } from '@/components/ui/spinner'
import { useToast } from '@/components/ui/toast-context'
import { useAiUsage, useRenameTenant } from '@/features/settings/api'
import { useCancelSubscription, useSubscriptions, type Subscription } from '@/features/subscription/api'
import { useWorkspace } from '@/features/workspace/workspace-context'
import { formatDate, formatMoney, formatNumber } from '@/lib/format'

const nameSchema = z.object({ name: z.string().trim().min(1, 'validation.required').max(200, 'validation.tooLong') })

export function PlanTab() {
  return (
    <div className="space-y-6">
      <AccountCard />
      <SubscriptionCard />
      <UsageCard />
    </div>
  )
}

function AccountCard() {
  const { t } = useTranslation()
  const toast = useToast()
  const session = useSession()
  const { tenant } = useWorkspace()
  const rename = useRenameTenant()
  const canOwn = can(session.tenant.role, 'owner')
  const { register, handleSubmit, formState } = useForm({ resolver: zodResolver(nameSchema), defaultValues: { name: tenant.name ?? '' } })

  const submit = handleSubmit((v) =>
    rename.mutateAsync(v.name).then(
      () => toast.success(t('common.saved')),
      (error) => toast.error(error),
    ),
  )

  return (
    <Card className="space-y-4 p-6">
      <div>
        <h2 className="text-xl font-bold">{t('settings.plan.accountTitle')}</h2>
        <p className="mt-1 text-sm text-muted">{canOwn ? t('settings.plan.accountHint') : t('settings.plan.ownerOnly')}</p>
      </div>
      <form onSubmit={submit} className="flex flex-wrap items-end gap-3" noValidate>
        <div className="min-w-60 flex-1">
          <Field label={t('settings.plan.accountName')} error={formState.errors.name?.message}>
            <Input disabled={!canOwn} {...register('name')} />
          </Field>
        </div>
        {canOwn && (
          <Button type="submit" disabled={rename.isPending}>
            {rename.isPending && <Spinner />}
            {t('common.save')}
          </Button>
        )}
      </form>
    </Card>
  )
}

function SubscriptionCard() {
  const { t, i18n } = useTranslation()
  const toast = useToast()
  const session = useSession()
  const { tenant } = useWorkspace()
  const subscriptions = useSubscriptions()
  const cancel = useCancelSubscription()
  const [canceling, setCanceling] = useState<Subscription | null>(null)
  const canOwn = can(session.tenant.role, 'owner')
  const current = tenant.currentSubscription

  const confirmCancel = () => {
    if (!canceling?.id) return
    cancel.mutate(canceling.id, {
      onSuccess: () => toast.success(t('settings.plan.canceled')),
      onError: (e) => toast.error(e),
      onSettled: () => setCanceling(null),
    })
  }

  return (
    <Card className="space-y-4 p-6">
      <h2 className="text-xl font-bold">{t('settings.plan.title')}</h2>
      {current ? (
        <div className="space-y-2 rounded-2xl bg-brand-100/60 p-4 dark:bg-brand-900/40">
          <div className="flex flex-wrap items-center gap-2">
            <p className="text-lg font-bold">{t(`plans.names.${current.plan ?? 'Basic'}`)}</p>
            <StatusBadge status={current.status} />
          </div>
          <p className="text-sm">{t('plans.period', { from: formatDate(current.startsAt, i18n.language), to: formatDate(current.endsAt, i18n.language) })}</p>
          <p className="text-sm text-muted">{t('plans.features.postsPerDay', { count: current.maxPostsPerDay ?? 0 })}</p>
          {canOwn && current.status?.code === 'Active' && (
            <Button variant="secondary" size="sm" onClick={() => setCanceling(current)}>{t('settings.plan.cancel')}</Button>
          )}
        </div>
      ) : (
        <p className="text-sm text-muted">{t('settings.plan.none')}</p>
      )}

      <div>
        <h3 className="mb-2 font-semibold">{t('settings.plan.history')}</h3>
        {subscriptions.isPending ? (
          <SectionSpinner />
        ) : subscriptions.isError ? (
          <ErrorState error={subscriptions.error} onRetry={() => void subscriptions.refetch()} />
        ) : (
          <ul className="divide-y divide-line rounded-2xl border border-line">
            {(subscriptions.data.items ?? []).map((s) => (
              <li key={s.id} className="flex flex-wrap items-center gap-3 p-3 text-sm">
                <span className="font-semibold">{t(`plans.names.${s.plan ?? 'Basic'}`)}</span>
                <span className="text-muted">{t(`plans.durations.${s.duration ?? 'OneMonth'}`)}</span>
                <span className="text-muted">{formatDate(s.startsAt, i18n.language)}</span>
                <span className="ms-auto font-semibold" dir="ltr">{formatMoney(s.price ?? 0, s.currency ?? 'USD', i18n.language)}</span>
                <StatusBadge status={s.status} />
              </li>
            ))}
          </ul>
        )}
      </div>

      <ConfirmDialog
        open={!!canceling}
        danger
        busy={cancel.isPending}
        title={t('settings.plan.cancelTitle')}
        description={t('settings.plan.cancelHint')}
        confirmLabel={t('settings.plan.cancel')}
        onConfirm={confirmCancel}
        onClose={() => setCanceling(null)}
      />
    </Card>
  )
}

const periods = [7, 30, 90] as const

function UsageCard() {
  const { t, i18n } = useTranslation()
  const [days, setDays] = useState<number>(30)
  const usage = useAiUsage(days)
  const stats = usage.data?.executions

  return (
    <Card className="space-y-4 p-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h2 className="text-xl font-bold">{t('settings.usage.title')}</h2>
          <p className="mt-1 text-sm text-muted">{t('settings.usage.subtitle')}</p>
        </div>
        <Select aria-label={t('settings.usage.period')} className="w-40" value={days} onChange={(e) => setDays(Number(e.target.value))}>
          {periods.map((p) => (
            <option key={p} value={p}>{t('settings.usage.lastDays', { count: p })}</option>
          ))}
        </Select>
      </div>
      {usage.isPending ? (
        <SectionSpinner />
      ) : usage.isError ? (
        <ErrorState error={usage.error} onRetry={() => void usage.refetch()} />
      ) : (
        <>
          <dl className="grid grid-cols-2 gap-3 sm:grid-cols-4">
            {[
              [t('settings.usage.total'), formatNumber(stats?.total ?? 0, i18n.language)],
              [t('settings.usage.succeeded'), formatNumber(stats?.succeeded ?? 0, i18n.language)],
              [t('settings.usage.failed'), formatNumber(stats?.failed ?? 0, i18n.language)],
              [t('settings.usage.cost'), formatMoney(usage.data.totalCostUsd ?? 0, 'USD', i18n.language)],
            ].map(([label, value]) => (
              <div key={label} className="rounded-2xl border border-line p-3">
                <dt className="text-xs text-muted">{label}</dt>
                <dd className="mt-1 text-xl font-bold" dir="ltr">{value}</dd>
              </div>
            ))}
          </dl>
          {(usage.data.byEventType ?? []).length > 0 && (
            <ul className="divide-y divide-line rounded-2xl border border-line">
              {usage.data.byEventType?.map((row) => (
                <li key={row.eventType} className="flex items-center gap-3 p-3 text-sm">
                  <span className="flex-1 font-semibold">{t(`settings.usage.events.${row.eventType}`, { defaultValue: row.eventType })}</span>
                  <span className="text-muted">{t('settings.usage.count', { count: row.events ?? 0 })}</span>
                  <span dir="ltr" className="w-20 text-end">{formatMoney(row.costUsd ?? 0, 'USD', i18n.language)}</span>
                </li>
              ))}
            </ul>
          )}
        </>
      )}
    </Card>
  )
}
