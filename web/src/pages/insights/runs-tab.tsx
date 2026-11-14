import { Sparkles, Telescope } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate, useSearchParams } from 'react-router'
import { can } from '@/auth/roles'
import { useSession } from '@/auth/use-session'
import { EmptyState } from '@/components/empty-state'
import { ErrorState } from '@/components/error-state'
import { Pagination } from '@/components/pagination'
import { StatusBadge } from '@/components/status-badge'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Field } from '@/components/ui/field'
import { Select } from '@/components/ui/select'
import { SectionSpinner, Spinner } from '@/components/ui/spinner'
import { useToast } from '@/components/ui/toast-context'
import { useCampaigns } from '@/features/campaigns/api'
import { runWindows, useStartStrategyRun, useStrategyRuns } from '@/features/insights/api'
import { useWorkspace } from '@/features/workspace/workspace-context'
import { cn } from '@/lib/cn'
import { formatDate, formatDateTime } from '@/lib/format'

const closedCampaigns = ['Completed', 'Canceled']

export function RunsTab() {
  const { t, i18n } = useTranslation()
  const toast = useToast()
  const navigate = useNavigate()
  const session = useSession()
  const { businessId, business } = useWorkspace()
  const [params, setParams] = useSearchParams()
  const page = Number(params.get('page') ?? 1)
  const runs = useStrategyRuns(businessId, page)
  const campaigns = useCampaigns(businessId)
  const start = useStartStrategyRun(businessId)
  const [windowDays, setWindowDays] = useState<number>(7)
  const [campaignId, setCampaignId] = useState('')
  const canEdit = can(session.tenant.role, 'edit')
  const openCampaigns = campaigns.data?.items?.filter((c) => !closedCampaigns.includes(c.status?.code ?? '')) ?? []
  const items = runs.data?.items ?? []

  const run = () =>
    start.mutate(
      { windowDays, campaignId: campaignId || undefined },
      {
        onSuccess: (r) => {
          toast.success(t('insights.runs.started', { count: r.recommendations?.length ?? 0 }))
          void navigate('/insights?tab=recommendations')
        },
        onError: (e) => toast.error(e),
      },
    )

  return (
    <>
      {canEdit && (
        <Card className="mb-6">
          <h2 className="font-bold">{t('insights.runs.newTitle')}</h2>
          <p className="mb-4 text-sm text-muted">{t('insights.runs.newHint')}</p>
          <div className="flex flex-wrap items-end gap-3">
            <div className="w-44">
              <Field label={t('insights.runs.window')}>
                <Select value={String(windowDays)} onChange={(e) => setWindowDays(Number(e.target.value))}>
                  {runWindows.map((d) => <option key={d} value={d}>{t('insights.runs.windowDays', { count: d })}</option>)}
                </Select>
              </Field>
            </div>
            <div className="w-56">
              <Field label={t('insights.runs.campaign')} optional>
                <Select value={campaignId} onChange={(e) => setCampaignId(e.target.value)}>
                  <option value="">{t('insights.runs.wholeBusiness')}</option>
                  {openCampaigns.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
                </Select>
              </Field>
            </div>
            <Button variant="sun" onClick={run} disabled={start.isPending}>
              {start.isPending ? <Spinner /> : <Sparkles aria-hidden />}
              {t('insights.runs.start')}
            </Button>
          </div>
        </Card>
      )}
      {runs.isPending ? (
        <SectionSpinner />
      ) : runs.isError ? (
        <ErrorState error={runs.error} onRetry={() => void runs.refetch()} />
      ) : items.length === 0 ? (
        <EmptyState icon={Telescope} title={t('insights.runs.empty')} hint={t('insights.runs.emptyHint')} />
      ) : (
        <>
          <ul className={cn('space-y-3', runs.isFetching && 'opacity-70')}>
            {items.map((r) => (
              <li key={r.id}>
                <Card className="flex flex-wrap items-center gap-3 p-4">
                  <div className="min-w-0">
                    <p className="font-semibold">{t('insights.runs.period', { from: formatDate(r.windowStart, i18n.language), to: formatDate(r.windowEnd, i18n.language) })}</p>
                    <p className="text-sm text-muted">
                      {r.completedAt ? t('insights.runs.completedAt', { when: formatDateTime(r.completedAt, i18n.language, business.timeZoneId ?? undefined) }) : t('insights.runs.notCompleted')}
                    </p>
                  </div>
                  <span className="ms-auto text-sm text-muted">{t('insights.runs.count', { count: r.recommendations?.length ?? 0 })}</span>
                  <StatusBadge status={r.status} />
                </Card>
              </li>
            ))}
          </ul>
          <Pagination
            page={page}
            totalPages={runs.data?.totalPages ?? 1}
            onChange={(p) => {
              const next = new URLSearchParams(params)
              next.set('page', String(p))
              setParams(next)
            }}
          />
        </>
      )}
    </>
  )
}
