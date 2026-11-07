import { ArrowRight, CalendarRange, Pencil, Plus, Sparkles, Wallet } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router'
import { can } from '@/auth/roles'
import { useSession } from '@/auth/use-session'
import { ConfirmDialog } from '@/components/confirm-dialog'
import { ErrorState } from '@/components/error-state'
import { StatusBadge } from '@/components/status-badge'
import { Alert } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { buttonVariants } from '@/components/ui/button-variants'
import { Card } from '@/components/ui/card'
import { SectionSpinner, Spinner } from '@/components/ui/spinner'
import { useToast } from '@/components/ui/toast-context'
import { campaignEditable, campaignNext, useCampaign, useCampaignPlans, useChangeCampaignStatus, useGeneratePlan, type CampaignPlan } from '@/features/campaigns/api'
import { CampaignFormDialog } from '@/features/campaigns/campaign-form-dialog'
import { useContentItems } from '@/features/content/api'
import { enumLabel } from '@/features/content/labels'
import { usePlatformName } from '@/features/platforms/api'
import { useWorkspace } from '@/features/workspace/workspace-context'
import { formatDate, formatDateTime, formatMoney } from '@/lib/format'

export function CampaignDetailPage() {
  const { id = '' } = useParams()
  const { t, i18n } = useTranslation()
  const toast = useToast()
  const session = useSession()
  const { businessId } = useWorkspace()
  const campaign = useCampaign(businessId, id)
  const changeStatus = useChangeCampaignStatus(businessId, id)
  const [editing, setEditing] = useState(false)
  const [confirmCancel, setConfirmCancel] = useState(false)
  const canEdit = can(session.tenant.role, 'edit')
  const move = (target: string) =>
    changeStatus.mutate(target, {
      onSuccess: () => {
        setConfirmCancel(false)
        toast.success(t('common.saved'))
      },
      onError: (err) => toast.error(err),
    })

  if (campaign.isPending) return <SectionSpinner />
  if (campaign.isError) return <ErrorState error={campaign.error} onRetry={() => void campaign.refetch()} />
  const c = campaign.data
  const code = c.status?.code ?? ''
  const isAdmin = can(session.tenant.role, 'admin')
  const next = (campaignNext[code] ?? []).filter((target) => target !== 'Active' || isAdmin)
  const editable = campaignEditable.includes(code)

  return (
    <div className="space-y-6">
      <div>
        <Link to="/campaigns" className="mb-3 inline-flex items-center gap-1 text-sm text-muted hover:text-ink">
          <ArrowRight className="size-4 ltr:rotate-180" aria-hidden />
          {t('campaigns.back')}
        </Link>
        <div className="flex flex-wrap items-center gap-3">
          <h1 className="text-2xl font-bold md:text-3xl">{c.name}</h1>
          <StatusBadge status={c.status} />
        </div>
        <p className="mt-2 max-w-3xl text-muted" dir="auto">{c.goalStatement}</p>
      </div>

      <Card className="flex flex-wrap items-center gap-x-8 gap-y-4 p-5">
        <Info icon={Sparkles} label={t('campaigns.objective')} value={enumLabel(t, 'objective', c.objective)} />
        <Info icon={Wallet} label={t('campaigns.budget')} value={c.budget != null ? formatMoney(c.budget, c.budgetCurrency ?? 'ILS', i18n.language) : '—'} />
        <Info icon={CalendarRange} label={t('campaigns.period')} value={c.startsAt ? `${formatDate(c.startsAt, i18n.language)} – ${formatDate(c.endsAt, i18n.language)}` : '—'} />
        {canEdit && (
          <div className="flex flex-1 flex-wrap justify-end gap-2">
            {next.map((target) => (
              <Button
                key={target}
                size="sm"
                variant={target === 'Active' ? 'primary' : target === 'Canceled' ? 'ghost' : 'secondary'}
                disabled={changeStatus.isPending}
                onClick={() => (target === 'Canceled' ? setConfirmCancel(true) : move(target))}
              >
                {t(`campaigns.moveTo.${target}`)}
              </Button>
            ))}
            {editable && (
              <Button variant="secondary" size="sm" onClick={() => setEditing(true)}>
                <Pencil aria-hidden />
                {t('common.edit')}
              </Button>
            )}
          </div>
        )}
      </Card>

      {code === 'Draft' && canEdit && <Alert tone="info" title={t('campaigns.draftHint')} />}
      <PlanSection campaignId={id} canEdit={canEdit && editable} />
      <CampaignPosts campaignId={id} canEdit={canEdit} />
      <CampaignFormDialog open={editing} onClose={() => setEditing(false)} campaign={c} />
      <ConfirmDialog
        open={confirmCancel}
        title={t('campaigns.cancelTitle')}
        description={t('campaigns.cancelHint')}
        confirmLabel={t('campaigns.moveTo.Canceled')}
        danger
        busy={changeStatus.isPending}
        onClose={() => setConfirmCancel(false)}
        onConfirm={() => move('Canceled')}
      />
    </div>
  )
}

function Info({ icon: Icon, label, value }: { icon: typeof Sparkles; label: string; value: string }) {
  return (
    <div className="flex items-center gap-3">
      <Icon className="size-5 text-brand-500" aria-hidden />
      <div>
        <p className="text-xs text-muted">{label}</p>
        <p className="font-semibold">{value}</p>
      </div>
    </div>
  )
}

function PlanSection({ campaignId, canEdit }: { campaignId: string; canEdit: boolean }) {
  const { t, i18n } = useTranslation()
  const toast = useToast()
  const { businessId } = useWorkspace()
  const plans = useCampaignPlans(businessId, campaignId)
  const generate = useGeneratePlan(businessId, campaignId)
  const platformName = usePlatformName()
  const [version, setVersion] = useState<number | null>(null)

  const all = [...(plans.data ?? [])].sort((a, b) => (b.versionNo ?? 0) - (a.versionNo ?? 0))
  const current = all.find((p) => p.versionNo === version) ?? all[0]
  const plan = (current?.plan ?? null) as CampaignPlan | null

  return (
    <Card className="overflow-hidden p-0">
      <div className="flex flex-wrap items-center justify-between gap-3 bg-linear-to-br from-grape-500 to-coral-400 p-5 text-white">
        <div>
          <h2 className="text-lg font-bold">{t('campaigns.plan.title')}</h2>
          <p className="text-sm text-white/85">{t('campaigns.plan.subtitle')}</p>
        </div>
        <div className="flex items-center gap-2">
          {all.length > 1 && (
            <select
              aria-label={t('campaigns.plan.version')}
              value={current?.versionNo ?? ''}
              onChange={(e) => setVersion(Number(e.target.value))}
              className="h-9 rounded-xl border-0 bg-white/20 px-3 text-sm font-semibold text-white"
            >
              {all.map((p) => <option key={p.id} value={p.versionNo} className="text-ink">{t('content.version', { n: p.versionNo })}</option>)}
            </select>
          )}
          {canEdit && (
            <Button variant="sun" size="sm" disabled={generate.isPending} onClick={() => generate.mutate(undefined, { onSuccess: () => { setVersion(null); toast.success(t('campaigns.plan.ready')) }, onError: (e) => toast.error(e) })}>
              {generate.isPending ? <Spinner /> : <Sparkles aria-hidden />}
              {all.length ? t('campaigns.plan.regenerate') : t('campaigns.plan.generate')}
            </Button>
          )}
        </div>
      </div>
      <div className="p-5">
        {plans.isPending ? (
          <SectionSpinner />
        ) : !plan ? (
          <p className="text-sm text-muted">{t('campaigns.plan.none')}</p>
        ) : (
          <div className="space-y-5">
            {plan.summary && <p className="text-lg font-semibold" dir="auto">{plan.summary}</p>}
            <div className="flex flex-wrap gap-2">
              {plan.durationDays != null && <Badge tone="grape">{t('campaigns.plan.days', { count: plan.durationDays })}</Badge>}
              {(plan.kpis ?? []).map((k) => <Badge key={k} tone="green">{k}</Badge>)}
              <span className="text-xs text-muted">{formatDateTime(current?.createdAt, i18n.language)}</span>
            </div>
            {(plan.channels ?? []).length > 0 && (
              <div>
                <h3 className="mb-2 font-semibold">{t('campaigns.plan.channels')}</h3>
                <ul className="grid gap-2 sm:grid-cols-2">
                  {plan.channels!.map((ch, i) => (
                    <li key={i} className="rounded-2xl bg-surface-2 p-4">
                      <p className="font-semibold">{platformName(ch.platform)}</p>
                      <p className="text-sm text-muted">
                        {t('campaigns.plan.perWeek', { count: ch.postsPerWeek ?? 0 })}
                        {(ch.bestTimes ?? []).length > 0 && ` · ${t('campaigns.plan.bestTimes', { times: ch.bestTimes!.join('، ') })}`}
                      </p>
                    </li>
                  ))}
                </ul>
              </div>
            )}
            {plan.contentMix && Object.keys(plan.contentMix).length > 0 && (
              <div>
                <h3 className="mb-2 font-semibold">{t('campaigns.plan.mix')}</h3>
                <div className="flex h-4 overflow-hidden rounded-full">
                  {Object.entries(plan.contentMix).map(([kind, percent], i) => (
                    <span key={kind} style={{ width: `${percent}%` }} className={['bg-brand-500', 'bg-sun-400', 'bg-grape-400', 'bg-coral-400', 'bg-lime-accent'][i % 5]} title={`${kind} ${percent}%`} />
                  ))}
                </div>
                <ul className="mt-2 flex flex-wrap gap-x-4 text-sm text-muted">
                  {Object.entries(plan.contentMix).map(([kind, percent]) => <li key={kind}>{enumLabel(t, 'contentType', kind)} {percent}%</li>)}
                </ul>
              </div>
            )}
            {(plan.weeklyThemes ?? []).length > 0 && (
              <div>
                <h3 className="mb-2 font-semibold">{t('campaigns.plan.themes')}</h3>
                <ol className="list-inside list-decimal space-y-1 text-sm" dir="auto">
                  {plan.weeklyThemes!.map((theme, i) => <li key={i}>{theme}</li>)}
                </ol>
              </div>
            )}
          </div>
        )}
      </div>
    </Card>
  )
}

function CampaignPosts({ campaignId, canEdit }: { campaignId: string; canEdit: boolean }) {
  const { t } = useTranslation()
  const { businessId } = useWorkspace()
  const items = useContentItems(businessId, { campaignId })
  const platformName = usePlatformName()
  return (
    <Card className="p-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-lg font-bold">{t('campaigns.posts')}</h2>
        <div className="flex gap-2">
          <Link to={`/content?campaign=${campaignId}`} className={buttonVariants({ variant: 'ghost', size: 'sm' })}>{t('home.more')}</Link>
          {canEdit && <Link to={`/content/new?campaign=${campaignId}`} className={buttonVariants({ variant: 'soft', size: 'sm' })}><Plus aria-hidden />{t('actions.createPost')}</Link>}
        </div>
      </div>
      {items.isPending ? (
        <SectionSpinner />
      ) : (items.data?.items ?? []).length === 0 ? (
        <p className="mt-4 text-sm text-muted">{t('campaigns.noPosts')}</p>
      ) : (
        <ul className="mt-4 divide-y divide-line">
          {items.data!.items!.map((item) => (
            <li key={item.id}>
              <Link to={`/content/${item.id}`} className="flex items-center gap-3 py-3 hover:opacity-80">
                <span className="min-w-0 flex-1">
                  <span className="block text-sm font-semibold">{platformName(item.platformCode)} · {enumLabel(t, 'contentType', item.contentType)}</span>
                  <span className="block truncate text-sm text-muted" dir="auto">{item.latestRevision?.body}</span>
                </span>
                <StatusBadge status={item.status} />
              </Link>
            </li>
          ))}
        </ul>
      )}
    </Card>
  )
}
