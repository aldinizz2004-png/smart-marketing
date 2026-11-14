import { Check, Lightbulb, Wand2, X } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router'
import { can } from '@/auth/roles'
import { useSession } from '@/auth/use-session'
import { EmptyState } from '@/components/empty-state'
import { ErrorState } from '@/components/error-state'
import { FilterChips } from '@/components/filter-chips'
import { Pagination } from '@/components/pagination'
import { StatusBadge } from '@/components/status-badge'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { SectionSpinner } from '@/components/ui/spinner'
import { useToast } from '@/components/ui/toast-context'
import { ApplyDialog } from '@/features/insights/apply-dialog'
import { readPayload, recommendationStatuses, useDecideRecommendation, useRecommendations, type Recommendation } from '@/features/insights/api'
import { usePlatformName } from '@/features/platforms/api'
import { useWorkspace } from '@/features/workspace/workspace-context'
import { cn } from '@/lib/cn'
import { formatDateTime, formatPercent } from '@/lib/format'

export function RecommendationsTab() {
  const { t, i18n } = useTranslation()
  const toast = useToast()
  const session = useSession()
  const { businessId, business } = useWorkspace()
  const platformName = usePlatformName()
  const [params, setParams] = useSearchParams()
  const status = params.get('status') ?? 'Pending'
  const page = Number(params.get('page') ?? 1)
  const list = useRecommendations(businessId, { status: status === 'all' ? '' : status, page })
  const decide = useDecideRecommendation(businessId)
  const [applying, setApplying] = useState<Recommendation | null>(null)
  const canEdit = can(session.tenant.role, 'edit')

  const update = (key: string, value: string) => {
    const next = new URLSearchParams(params)
    next.set(key, value)
    if (key !== 'page') next.delete('page')
    setParams(next)
  }

  const decideNow = (r: Recommendation, value: 'Accepted' | 'Rejected') =>
    decide.mutate(
      { id: r.id!, status: value },
      { onSuccess: () => toast.success(value === 'Accepted' ? t('insights.recommendations.accepted') : t('insights.recommendations.rejected')), onError: (e) => toast.error(e) },
    )

  const items = list.data?.items ?? []

  return (
    <>
      <div className="mb-5">
        <FilterChips
          label={t('content.filterStatus')}
          value={status}
          onChange={(v) => update('status', v)}
          options={[{ value: 'all', label: t('common.all') }, ...recommendationStatuses.map((s) => ({ value: s, label: t(`insights.recommendations.statuses.${s}`) }))]}
        />
      </div>
      {list.isPending ? (
        <SectionSpinner />
      ) : list.isError ? (
        <ErrorState error={list.error} onRetry={() => void list.refetch()} />
      ) : items.length === 0 ? (
        <EmptyState icon={Lightbulb} title={status === 'Pending' ? t('insights.recommendations.emptyPending') : t('insights.recommendations.empty')} hint={t('insights.recommendations.emptyHint')} />
      ) : (
        <>
          <ul className={cn('space-y-3', list.isFetching && 'opacity-70')}>
            {items.map((r) => {
              const payload = readPayload(r.proposedPayload)
              const code = r.status?.code
              return (
                <li key={r.id}>
                  <Card className="space-y-3">
                    <div className="flex flex-wrap items-center gap-2">
                      <Badge tone="green">{t(`insights.recommendations.actions.${r.actionType ?? 'NewContent'}`)}</Badge>
                      {payload.platform && <Badge tone="gray">{platformName(payload.platform)}</Badge>}
                      {payload.format && <Badge tone="grape">{t(`enums.format.${payload.format}`, { defaultValue: payload.format })}</Badge>}
                      {payload.hook && <Badge tone="sun">{t(`enums.hook.${payload.hook}`, { defaultValue: payload.hook })}</Badge>}
                      <StatusBadge status={r.status} />
                      {r.appliedAutomatically && <Badge tone="gray">{t('insights.recommendations.auto')}</Badge>}
                      {r.confidence != null && <span className="ms-auto text-xs text-muted">{t('insights.recommendations.confidence', { value: formatPercent(r.confidence, i18n.language, 0) })}</span>}
                    </div>
                    {payload.captionIdea && <p dir="auto" className="font-semibold [overflow-wrap:anywhere]">{payload.captionIdea}</p>}
                    {r.rationale && <p dir="auto" className="text-muted [overflow-wrap:anywhere]">{r.rationale}</p>}
                    {r.proposedPublishAt && <p className="text-sm text-muted">{t('insights.recommendations.proposedAt', { when: formatDateTime(r.proposedPublishAt, i18n.language, business.timeZoneId ?? undefined) })}</p>}
                    {(r.evidence ?? []).some((e) => e.interpretation) && (
                      <details className="rounded-xl bg-surface-2 p-3 text-sm">
                        <summary className="cursor-pointer font-semibold">{t('insights.recommendations.evidence')}</summary>
                        <ul className="mt-2 list-disc space-y-1 ps-5 text-muted">
                          {r.evidence!.filter((e) => e.interpretation).map((e, i) => <li key={i} dir="auto" className="[overflow-wrap:anywhere]">{e.interpretation}</li>)}
                        </ul>
                      </details>
                    )}
                    {canEdit && (code === 'Pending' || (code === 'Accepted' && r.actionType === 'NewContent')) && (
                      <div className="flex flex-wrap gap-2 pt-1">
                        {code === 'Pending' && (
                          <>
                            <Button size="sm" disabled={decide.isPending} onClick={() => decideNow(r, 'Accepted')}>
                              <Check aria-hidden />
                              {t('insights.recommendations.accept')}
                            </Button>
                            <Button size="sm" variant="secondary" disabled={decide.isPending} onClick={() => decideNow(r, 'Rejected')}>
                              <X aria-hidden />
                              {t('insights.recommendations.reject')}
                            </Button>
                          </>
                        )}
                        {code === 'Accepted' && r.actionType === 'NewContent' && (
                          <Button size="sm" variant="sun" onClick={() => setApplying(r)}>
                            <Wand2 aria-hidden />
                            {t('insights.recommendations.writePost')}
                          </Button>
                        )}
                      </div>
                    )}
                  </Card>
                </li>
              )
            })}
          </ul>
          <Pagination page={page} totalPages={list.data?.totalPages ?? 1} onChange={(p) => update('page', String(p))} />
        </>
      )}
      <ApplyDialog recommendation={applying} onClose={() => setApplying(null)} />
    </>
  )
}
