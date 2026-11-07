import { useQueryClient } from '@tanstack/react-query'
import { Check, LogOut } from 'lucide-react'
import { useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Navigate, useNavigate } from 'react-router'
import { Brand } from '@/app/layout/brand'
import { LanguageToggle } from '@/app/layout/language-toggle'
import { logout } from '@/auth/auth-api'
import { can } from '@/auth/roles'
import { useSession } from '@/auth/use-session'
import { ErrorState } from '@/components/error-state'
import { Alert } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { FullPageSpinner } from '@/components/ui/spinner'
import { useToast } from '@/components/ui/toast-context'
import { useSaveBrandProfile } from '@/features/brand/api'
import { BrandProfileForm } from '@/features/brand/brand-profile-form'
import { BusinessForm } from '@/features/business/business-form'
import { ConnectAccountForm } from '@/features/social/connect-account-form'
import { useSocialAccounts } from '@/features/social/api'
import { SocialAccountsList } from '@/features/social/social-accounts-list'
import { useActivateSubscription, useCancelSubscription, useCreateSubscription, useSubscriptions } from '@/features/subscription/api'
import { PaymentForm } from '@/features/subscription/payment-form'
import { PlanPicker } from '@/features/subscription/plan-picker'
import { useBusinesses, useCreateBusiness, useTenant } from '@/features/workspace/api'
import { cn } from '@/lib/cn'

const steps = ['plan', 'business', 'brand', 'accounts'] as const
type Step = (typeof steps)[number]

export function OnboardingPage() {
  const { t } = useTranslation()
  const session = useSession()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const tenant = useTenant()
  const hasAccess = tenant.data?.currentSubscription?.grantsAccessNow === true
  const businesses = useBusinesses(hasAccess)
  const [createdBusinessId, setCreatedBusinessId] = useState<string | null>(null)
  const [laterStep, setLaterStep] = useState<'brand' | 'accounts'>('brand')

  if (tenant.isPending || (hasAccess && businesses.isPending)) return <FullPageSpinner />
  if (tenant.isError) return <Centered><ErrorState error={tenant.error} onRetry={() => void tenant.refetch()} /></Centered>
  if (businesses.isError) return <Centered><ErrorState error={businesses.error} onRetry={() => void businesses.refetch()} /></Centered>

  const hasBusiness = (businesses.data?.items ?? []).some((b) => b.status?.code !== 'Closed')
  if (hasAccess && hasBusiness && !createdBusinessId) return <Navigate to="/" replace />

  const step: Step = !hasAccess ? 'plan' : !createdBusinessId ? 'business' : laterStep
  const finish = () => navigate('/', { replace: true })

  async function signOut() {
    await logout().catch(() => undefined)
    queryClient.clear()
    navigate('/login', { replace: true })
  }

  return (
    <div className="min-h-dvh">
      <header className="flex items-center justify-between px-5 py-4 sm:px-10">
        <Brand compact />
        <div className="flex items-center gap-1">
          <LanguageToggle />
          <Button variant="ghost" size="sm" onClick={() => void signOut()} aria-label={t('auth.logout')}>
            <LogOut aria-hidden />
            <span className="hidden sm:inline">{t('auth.logout')}</span>
          </Button>
        </div>
      </header>

      <main className="mx-auto max-w-5xl px-4 pb-16 sm:px-8">
        <Stepper current={step} />
        <div className="mt-8">
          {step === 'plan' && <PlanStep canPay={can(session.tenant.role, 'owner')} />}
          {step === 'business' && (
            <StepCard title={t('onboarding.business.title')} subtitle={t('onboarding.business.subtitle')}>
              {can(session.tenant.role, 'admin') ? (
                <BusinessStep onCreated={setCreatedBusinessId} />
              ) : (
                <Alert tone="info" title={t('onboarding.business.askAdmin')} />
              )}
            </StepCard>
          )}
          {step === 'brand' && createdBusinessId && (
            <BrandStep businessId={createdBusinessId} onDone={() => setLaterStep('accounts')} />
          )}
          {step === 'accounts' && createdBusinessId && <AccountsStep businessId={createdBusinessId} onDone={finish} />}
        </div>
      </main>
    </div>
  )
}

function Centered({ children }: { children: ReactNode }) {
  return <div className="mx-auto grid min-h-dvh max-w-lg place-items-center p-6">{children}</div>
}

function Stepper({ current }: { current: Step }) {
  const { t } = useTranslation()
  const index = steps.indexOf(current)
  return (
    <ol className="flex items-center gap-2 sm:gap-4" aria-label={t('onboarding.progress')}>
      {steps.map((s, i) => (
        <li key={s} className="flex flex-1 items-center gap-2" aria-current={i === index ? 'step' : undefined}>
          <span
            className={cn(
              'grid size-8 shrink-0 place-items-center rounded-full text-sm font-bold',
              i < index && 'bg-brand-500 text-white',
              i === index && 'bg-linear-to-br from-sun-400 to-coral-400 text-white shadow-md',
              i > index && 'bg-surface-2 text-muted',
            )}
          >
            {i < index ? <Check className="size-4" aria-hidden /> : i + 1}
          </span>
          <span className={cn('hidden text-sm font-semibold sm:block', i === index ? 'text-ink' : 'text-muted')}>{t(`onboarding.steps.${s}`)}</span>
          {i < steps.length - 1 && <span className={cn('h-0.5 flex-1 rounded-full', i < index ? 'bg-brand-500' : 'bg-line')} aria-hidden />}
        </li>
      ))}
    </ol>
  )
}

function StepCard({ title, subtitle, children }: { title: string; subtitle: string; children: ReactNode }) {
  return (
    <Card className="mx-auto max-w-3xl p-6 sm:p-8">
      <h1 className="text-2xl font-bold">{title}</h1>
      <p className="mt-1 text-muted">{subtitle}</p>
      <div className="mt-6">{children}</div>
    </Card>
  )
}

function PlanStep({ canPay }: { canPay: boolean }) {
  const { t } = useTranslation()
  const toast = useToast()
  const subscriptions = useSubscriptions()
  const create = useCreateSubscription()
  const activate = useActivateSubscription()
  const cancel = useCancelSubscription()

  const pending = subscriptions.data?.items?.find((s) => s.status?.code === 'Pending')

  if (!canPay)
    return (
      <StepCard title={t('onboarding.plan.title')} subtitle={t('onboarding.plan.subtitle')}>
        <Alert tone="info" title={t('onboarding.plan.askOwner')} />
      </StepCard>
    )

  return (
    <div>
      <div className="mb-8 text-center">
        <h1 className="text-3xl font-bold">{pending ? t('onboarding.plan.payTitle') : t('onboarding.plan.title')}</h1>
        <p className="mt-2 text-muted">{pending ? t('onboarding.plan.paySubtitle') : t('onboarding.plan.subtitle')}</p>
      </div>
      {subscriptions.isPending ? null : pending ? (
        <PaymentForm
          subscription={pending}
          changing={cancel.isPending}
          onChangePlan={() => cancel.mutate(pending.id ?? '', { onError: (e) => toast.error(e) })}
          onPay={async (paymentReference) => {
            try {
              await activate.mutateAsync({ id: pending.id ?? '', paymentReference })
              toast.success(t('onboarding.plan.activated'))
            } catch (error) {
              toast.error(error)
            }
          }}
        />
      ) : (
        <PlanPicker
          busyPlan={create.isPending ? create.variables?.plan : null}
          onChoose={(plan, duration) => create.mutate({ plan, duration }, { onError: (e) => toast.error(e) })}
        />
      )}
    </div>
  )
}

function BusinessStep({ onCreated }: { onCreated: (id: string) => void }) {
  const { t } = useTranslation()
  const toast = useToast()
  const create = useCreateBusiness()
  return (
    <BusinessForm
      submitLabel={t('onboarding.business.submit')}
      onSubmit={async (request) => {
        try {
          const business = await create.mutateAsync(request)
          onCreated(business.id ?? '')
        } catch (error) {
          toast.error(error)
        }
      }}
    />
  )
}

function BrandStep({ businessId, onDone }: { businessId: string; onDone: () => void }) {
  const { t } = useTranslation()
  const toast = useToast()
  const save = useSaveBrandProfile(businessId)
  return (
    <StepCard title={t('onboarding.brand.title')} subtitle={t('onboarding.brand.subtitle')}>
      <BrandProfileForm
        submitLabel={t('onboarding.next')}
        secondaryAction={<Button type="button" variant="ghost" size="lg" onClick={onDone}>{t('onboarding.skip')}</Button>}
        onSubmit={async (request) => {
          try {
            await save.mutateAsync(request)
            onDone()
          } catch (error) {
            toast.error(error)
          }
        }}
      />
    </StepCard>
  )
}

function AccountsStep({ businessId, onDone }: { businessId: string; onDone: () => void }) {
  const { t } = useTranslation()
  const accounts = useSocialAccounts(businessId)
  const count = accounts.data?.length ?? 0
  return (
    <StepCard title={t('onboarding.accounts.title')} subtitle={t('onboarding.accounts.subtitle')}>
      <div className="space-y-6">
        <SocialAccountsList accounts={accounts.data ?? []} />
        <ConnectAccountForm businessId={businessId} />
        <div className="flex justify-end border-t border-line pt-5">
          <Button size="lg" variant={count > 0 ? 'primary' : 'secondary'} onClick={onDone}>
            {count > 0 ? t('onboarding.finish') : t('onboarding.skipFinish')}
          </Button>
        </div>
      </div>
    </StepCard>
  )
}
