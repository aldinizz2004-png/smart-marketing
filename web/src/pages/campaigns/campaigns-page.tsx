import { Megaphone, Plus, Target } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useSearchParams } from 'react-router'
import { can } from '@/auth/roles'
import { useSession } from '@/auth/use-session'
import { EmptyState } from '@/components/empty-state'
import { ErrorState } from '@/components/error-state'
import { FilterChips } from '@/components/filter-chips'
import { PageHeader } from '@/components/page-header'
import { StatusBadge } from '@/components/status-badge'
import { Button } from '@/components/ui/button'
import { SectionSpinner } from '@/components/ui/spinner'
import { campaignStatuses, useCampaigns } from '@/features/campaigns/api'
import { CampaignFormDialog } from '@/features/campaigns/campaign-form-dialog'
import { enumLabel } from '@/features/content/labels'
import { useWorkspace } from '@/features/workspace/workspace-context'
import { formatDate, formatMoney } from '@/lib/format'

export function CampaignsPage() {
  const { t, i18n } = useTranslation()
  const session = useSession()
  const { businessId } = useWorkspace()
  const [params, setParams] = useSearchParams()
  const status = params.get('status') ?? ''
  const campaigns = useCampaigns(businessId, { status })
  const [creating, setCreating] = useState(false)
  const canEdit = can(session.tenant.role, 'edit')

  return (
    <>
      <PageHeader
        title={t('campaigns.title')}
        subtitle={t('campaigns.subtitle')}
        actions={canEdit && <Button onClick={() => setCreating(true)}><Plus aria-hidden />{t('campaigns.new')}</Button>}
      />
      <div className="mb-5">
        <FilterChips
          label={t('content.filterStatus')}
          value={status}
          onChange={(v) => setParams(v ? { status: v } : {})}
          options={[{ value: '', label: t('common.all') }, ...campaignStatuses.map((s) => ({ value: s, label: t(`campaigns.statuses.${s}`) }))]}
        />
      </div>
      {campaigns.isPending ? (
        <SectionSpinner />
      ) : campaigns.isError ? (
        <ErrorState error={campaigns.error} onRetry={() => void campaigns.refetch()} />
      ) : (campaigns.data.items ?? []).length === 0 ? (
        <EmptyState
          icon={Megaphone}
          title={t('campaigns.empty')}
          hint={t('campaigns.emptyHint')}
          action={canEdit && <Button onClick={() => setCreating(true)}><Plus aria-hidden />{t('campaigns.new')}</Button>}
        />
      ) : (
        <ul className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          {campaigns.data.items!.map((c) => (
            <li key={c.id}>
              <Link to={`/campaigns/${c.id}`} className="flex h-full flex-col gap-3 rounded-2xl border border-line bg-surface p-5 shadow-sm transition-all hover:-translate-y-0.5 hover:shadow-md">
                <div className="flex items-start justify-between gap-3">
                  <span className="grid size-11 place-items-center rounded-xl bg-linear-to-br from-coral-400 to-sun-400 text-white">
                    <Target className="size-5" aria-hidden />
                  </span>
                  <StatusBadge status={c.status} />
                </div>
                <h2 className="text-lg font-bold">{c.name}</h2>
                <p className="line-clamp-2 flex-1 text-sm text-muted" dir="auto">{c.goalStatement}</p>
                <div className="flex flex-wrap gap-x-4 gap-y-1 text-xs text-muted">
                  <span>{enumLabel(t, 'objective', c.objective)}</span>
                  {c.budget != null && <span>{formatMoney(c.budget, c.budgetCurrency ?? 'ILS', i18n.language)}</span>}
                  {c.startsAt && <span>{formatDate(c.startsAt, i18n.language)} – {formatDate(c.endsAt, i18n.language)}</span>}
                </div>
              </Link>
            </li>
          ))}
        </ul>
      )}
      <CampaignFormDialog open={creating} onClose={() => setCreating(false)} />
    </>
  )
}
