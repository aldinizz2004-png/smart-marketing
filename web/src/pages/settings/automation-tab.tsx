import { zodResolver } from '@hookform/resolvers/zod'
import { Controller, useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { ApiError } from '@/api/errors'
import { can } from '@/auth/roles'
import { useSession } from '@/auth/use-session'
import { ErrorState } from '@/components/error-state'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Field } from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { SectionSpinner, Spinner } from '@/components/ui/spinner'
import { useToast } from '@/components/ui/toast-context'
import { usePlatformName, usePlatforms } from '@/features/platforms/api'
import {
  recommendationActions,
  useAutomationPolicy,
  useSaveAutomationPolicy,
  type AutomationPolicy,
  type RecommendationAction,
} from '@/features/settings/api'
import { useWorkspace } from '@/features/workspace/workspace-context'
import { currencies } from '@/lib/options'

const optionalNumber = z.string().trim().regex(/^$|^\d+(\.\d+)?$/, 'validation.number')

const schema = z.object({
  automationEnabled: z.boolean(),
  maxPostsPerDay: z.string().trim().regex(/^\d+$/, 'validation.number'),
  publishWindowStart: z.string(),
  publishWindowEnd: z.string(),
  maxDiscountPercent: optionalNumber,
  monthlyAdBudget: optionalNumber,
  budgetCurrency: z.string(),
  requireSeparateReviewer: z.boolean(),
  autoReplyToMessages: z.boolean(),
  allowedPlatforms: z.array(z.string()),
  autoApplyActions: z.array(z.string()),
})

type Values = z.infer<typeof schema>

const hhmm = (value?: string | null) => (value ? value.slice(0, 5) : '')
const withSeconds = (value: string) => (value === '' ? null : value.length === 5 ? `${value}:00` : value)
const numberOrNull = (value: string) => (value.trim() === '' ? null : Number(value))

function toValues(policy: AutomationPolicy): Values {
  return {
    automationEnabled: policy.automationEnabled ?? false,
    maxPostsPerDay: String(policy.maxPostsPerDay ?? policy.planMaxPostsPerDay ?? 1),
    publishWindowStart: hhmm(policy.publishWindowStart),
    publishWindowEnd: hhmm(policy.publishWindowEnd),
    maxDiscountPercent: policy.maxDiscountPercent == null ? '' : String(policy.maxDiscountPercent),
    monthlyAdBudget: policy.monthlyAdBudget == null ? '' : String(policy.monthlyAdBudget),
    budgetCurrency: policy.budgetCurrency ?? '',
    requireSeparateReviewer: policy.requireSeparateReviewer ?? false,
    autoReplyToMessages: policy.autoReplyToMessages ?? false,
    allowedPlatforms: policy.allowedPlatforms ?? [],
    autoApplyActions: policy.autoApplyActions ?? [],
  }
}

export function AutomationTab() {
  const { businessId } = useWorkspace()
  const policy = useAutomationPolicy(businessId)

  if (policy.isPending) return <SectionSpinner />
  if (policy.isError) return <ErrorState error={policy.error} onRetry={() => void policy.refetch()} />
  return <PolicyForm key={`${businessId}-${policy.data.updatedAt ?? 'new'}`} businessId={businessId} policy={policy.data} />
}

function PolicyForm({ businessId, policy }: { businessId: string; policy: AutomationPolicy }) {
  const { t } = useTranslation()
  const toast = useToast()
  const session = useSession()
  const platforms = usePlatforms()
  const platformName = usePlatformName()
  const save = useSaveAutomationPolicy(businessId)
  const canAdmin = can(session.tenant.role, 'admin')
  const { register, handleSubmit, control, formState } = useForm<Values>({ resolver: zodResolver(schema), defaultValues: toValues(policy) })
  const planMax = policy.planMaxPostsPerDay ?? 0

  const submit = handleSubmit((v) => {
    const perDay = Number(v.maxPostsPerDay)
    if (planMax > 0 && perDay > planMax) {
      toast.error(new ApiError(400, { detail: t('settings.automation.overPlan', { count: planMax }) }))
      return undefined
    }
    return save
      .mutateAsync({
        automationEnabled: v.automationEnabled,
        maxPostsPerDay: perDay,
        publishWindowStart: withSeconds(v.publishWindowStart),
        publishWindowEnd: withSeconds(v.publishWindowEnd),
        maxDiscountPercent: numberOrNull(v.maxDiscountPercent),
        monthlyAdBudget: numberOrNull(v.monthlyAdBudget),
        budgetCurrency: v.budgetCurrency === '' ? null : v.budgetCurrency,
        requireSeparateReviewer: v.requireSeparateReviewer,
        autoReplyToMessages: v.autoReplyToMessages,
        allowedPlatforms: v.allowedPlatforms,
        autoApplyActions: v.autoApplyActions as RecommendationAction[],
      })
      .then(
        () => toast.success(t('common.saved')),
        (error) => toast.error(error),
      )
  })

  const toggles = [
    { name: 'automationEnabled', label: t('settings.automation.enabled'), hint: t('settings.automation.enabledHint') },
    { name: 'requireSeparateReviewer', label: t('settings.automation.separateReviewer'), hint: t('settings.automation.separateReviewerHint') },
    { name: 'autoReplyToMessages', label: t('settings.automation.autoReply'), hint: t('settings.automation.autoReplyHint') },
  ] as const

  return (
    <Card className="p-6">
      <form onSubmit={submit} className="space-y-6" noValidate>
        <div>
          <h2 className="text-xl font-bold">{t('settings.automation.title')}</h2>
          <p className="mt-1 text-sm text-muted">{canAdmin ? t('settings.automation.subtitle') : t('settings.automation.adminOnly')}</p>
        </div>
        <fieldset disabled={!canAdmin || formState.isSubmitting} className="space-y-6">
          <div className="space-y-3">
            {toggles.map((toggle) => (
              <div key={toggle.name} className="flex items-start gap-3 rounded-2xl border border-line p-4">
                <input id={toggle.name} type="checkbox" className="mt-1 size-4 accent-brand-600" aria-describedby={`${toggle.name}-hint`} {...register(toggle.name)} />
                <div>
                  <label htmlFor={toggle.name} className="block cursor-pointer font-semibold">{toggle.label}</label>
                  <p id={`${toggle.name}-hint`} className="text-sm text-muted">{toggle.hint}</p>
                </div>
              </div>
            ))}
          </div>

          <div className="grid gap-5 sm:grid-cols-2">
            <Field label={t('settings.automation.maxPosts')} hint={t('settings.automation.maxPostsHint', { count: planMax })} error={formState.errors.maxPostsPerDay?.message}>
              <Input inputMode="numeric" dir="ltr" {...register('maxPostsPerDay')} />
            </Field>
            <Field label={t('settings.automation.maxDiscount')} optional error={formState.errors.maxDiscountPercent?.message}>
              <Input inputMode="decimal" dir="ltr" {...register('maxDiscountPercent')} />
            </Field>
            <Field label={t('settings.automation.windowStart')} optional>
              <Input type="time" dir="ltr" {...register('publishWindowStart')} />
            </Field>
            <Field label={t('settings.automation.windowEnd')} optional>
              <Input type="time" dir="ltr" {...register('publishWindowEnd')} />
            </Field>
            <Field label={t('settings.automation.budget')} optional error={formState.errors.monthlyAdBudget?.message}>
              <Input inputMode="decimal" dir="ltr" {...register('monthlyAdBudget')} />
            </Field>
            <Field label={t('settings.automation.budgetCurrency')} optional>
              <Select dir="ltr" {...register('budgetCurrency')}>
                <option value="">â€”</option>
                {currencies.map((c) => (
                  <option key={c} value={c}>{c}</option>
                ))}
              </Select>
            </Field>
          </div>

          <Controller
            control={control}
            name="allowedPlatforms"
            render={({ field }) => (
              <CheckGroup
                legend={t('settings.automation.platforms')}
                hint={t('settings.automation.platformsHint')}
                options={(platforms.data ?? []).filter((p) => p.supportsPublishing).map((p) => ({ value: p.code ?? '', label: platformName(p.code) }))}
                value={field.value}
                onChange={field.onChange}
              />
            )}
          />
          <Controller
            control={control}
            name="autoApplyActions"
            render={({ field }) => (
              <CheckGroup
                legend={t('settings.automation.autoApply')}
                hint={t('settings.automation.autoApplyHint')}
                options={recommendationActions.map((a) => ({ value: a, label: t(`insights.recommendations.actions.${a}`) }))}
                value={field.value}
                onChange={field.onChange}
              />
            )}
          />
        </fieldset>
        {canAdmin && (
          <Button type="submit" size="lg" disabled={formState.isSubmitting}>
            {formState.isSubmitting && <Spinner />}
            {t('common.save')}
          </Button>
        )}
      </form>
    </Card>
  )
}

type CheckGroupProps = {
  legend: string
  hint: string
  options: { value: string; label: string }[]
  value: string[]
  onChange: (value: string[]) => void
}

function CheckGroup({ legend, hint, options, value, onChange }: CheckGroupProps) {
  const toggle = (item: string) => onChange(value.includes(item) ? value.filter((v) => v !== item) : [...value, item])
  return (
    <fieldset className="space-y-2">
      <legend className="text-sm font-semibold">{legend}</legend>
      <p className="text-sm text-muted">{hint}</p>
      <div className="flex flex-wrap gap-2">
        {options.map((option) => (
          <label key={option.value} className="flex cursor-pointer items-center gap-2 rounded-xl border border-line px-3 py-2 text-sm has-checked:border-brand-500 has-checked:bg-brand-50 dark:has-checked:bg-brand-900/40">
            <input type="checkbox" className="size-4 accent-brand-600" checked={value.includes(option.value)} onChange={() => toggle(option.value)} />
            {option.label}
          </label>
        ))}
      </div>
    </fieldset>
  )
}
