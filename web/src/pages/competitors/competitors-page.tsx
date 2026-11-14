import { Binoculars, Lightbulb, Pause, Play, Plus, RefreshCw, Sparkles, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router'
import { ApiError } from '@/api/errors'
import { can } from '@/auth/roles'
import { useSession } from '@/auth/use-session'
import { ConfirmDialog } from '@/components/confirm-dialog'
import { EmptyState } from '@/components/empty-state'
import { ErrorState } from '@/components/error-state'
import { FilterChips } from '@/components/filter-chips'
import { PageHeader } from '@/components/page-header'
import { StatusBadge } from '@/components/status-badge'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { SectionSpinner, Spinner } from '@/components/ui/spinner'
import { useToast } from '@/components/ui/toast-context'
import {
  sourceKinds,
  useAnalyzeInsights,
  useChangeSourceStatus,
  useCompetitorInsights,
  useDeleteSource,
  useScanSource,
  useTrackedSources,
  type TrackedSource,
} from '@/features/competitors/api'
import { SourceFormDialog } from '@/features/competitors/source-form-dialog'
import { SourcePostsDialog } from '@/features/competitors/source-posts-dialog'
import { useWorkspace } from '@/features/workspace/workspace-context'
import { cn } from '@/lib/cn'
import { formatPercent, formatRelative } from '@/lib/format'

const kindTones = { Competitor: 'coral', Professional: 'grape', Inspiration: 'sun' } as const

export function CompetitorsPage() {
  const { t, i18n } = useTranslation()
  const toast = useToast()
  const session = useSession()
  const { businessId } = useWorkspace()
  const [params, setParams] = useSearchParams()
  const kind = params.get('kind') ?? ''
  const sources = useTrackedSources(businessId, kind)
  const insights = useCompetitorInsights(businessId)
  const scan = useScanSource(businessId)
  const changeStatus = useChangeSourceStatus(businessId)
  const remove = useDeleteSource(businessId)
  const analyze = useAnalyzeInsights(businessId)
  const [adding, setAdding] = useState(false)
  const [viewing, setViewing] = useState<TrackedSource | null>(null)
  const [deleting, setDeleting] = useState<TrackedSource | null>(null)
  const [scanningId, setScanningId] = useState<string | null>(null)
  const canEdit = can(session.tenant.role, 'edit')

  const items = sources.data?.items ?? []
  const hasPosts = items.some((s) => (s.posts ?? 0) > 0)
  const names = new Map(items.map((s) => [s.id, s.displayName || s.handle || s.url]))

  const setKind = (value: string) => {
    const next = new URLSearchParams(params)
    if (value) next.set('kind', value)
    else next.delete('kind')
    setParams(next)
  }

  const runScan = (source: TrackedSource) => {
    setScanningId(source.id ?? null)
    scan.mutate(source.id!, {
      onSuccess: (r) => (r.error ? toast.error(new ApiError(502, { detail: r.error })) : toast.success(t('competitors.scanned', { added: r.added ?? 0, updated: r.updated ?? 0 }))),
      onError: (e) => toast.error(e),
      onSettled: () => setScanningId(null),
    })
  }

  const toggle = (source: TrackedSource) => {
    const active = source.status?.code === 'Active'
    changeStatus.mutate(
      { id: source.id!, status: active ? 'Paused' : 'Active' },
      { onSuccess: () => toast.success(active ? t('competitors.paused') : t('competitors.resumed')), onError: (e) => toast.error(e) },
    )
  }

  const confirmDelete = () => {
    if (!deleting?.id) return
    remove.mutate(deleting.id, {
      onSuccess: () => {
        toast.success(t('competitors.deleted'))
        setDeleting(null)
      },
      onError: (e) => {
        toast.error(e)
        setDeleting(null)
      },
    })
  }

  return (
    <>
      <PageHeader
        title={t('competitors.title')}
        subtitle={t('competitors.subtitle')}
        actions={
          canEdit && (
            <div className="flex flex-wrap gap-2">
              <Button
                variant="sun"
                disabled={analyze.isPending || !hasPosts}
                onClick={() =>
                  analyze.mutate(undefined, {
                    onSuccess: (r) => toast.success(t('competitors.analyzed', { count: r.insights?.length ?? 0, posts: r.postsAnalyzed ?? 0 })),
                    onError: (e) => toast.error(e),
                  })
                }
              >
                {analyze.isPending ? <Spinner /> : <Sparkles aria-hidden />}
                {t('competitors.analyze')}
              </Button>
              <Button onClick={() => setAdding(true)}>
                <Plus aria-hidden />
                {t('competitors.newSource')}
              </Button>
            </div>
          )
        }
      />

      <div className="mb-5">
        <FilterChips
          label={t('competitors.filterKind')}
          value={kind}
          onChange={setKind}
          options={[{ value: '', label: t('common.all') }, ...sourceKinds.map((k) => ({ value: k, label: t(`competitors.kinds.${k}`) }))]}
        />
      </div>

      {sources.isPending ? (
        <SectionSpinner />
      ) : sources.isError ? (
        <ErrorState error={sources.error} onRetry={() => void sources.refetch()} />
      ) : items.length === 0 ? (
        <EmptyState
          icon={Binoculars}
          title={kind ? t('competitors.emptyFiltered') : t('competitors.empty')}
          hint={t('competitors.emptyHint')}
          action={canEdit && !kind && <Button onClick={() => setAdding(true)}><Plus aria-hidden />{t('competitors.newSource')}</Button>}
        />
      ) : (
        <ul className="grid gap-4 lg:grid-cols-2">
          {items.map((source) => {
            const active = source.status?.code === 'Active'
            const scanning = scanningId === source.id
            const name = source.displayName || source.handle || source.url
            return (
              <li key={source.id}>
                <Card className={cn('h-full space-y-3', !active && 'opacity-80')}>
                  <div className="flex flex-wrap items-start justify-between gap-2">
                    <div className="min-w-0">
                      <h2 className="truncate text-lg font-bold">{name}</h2>
                      <a href={source.url ?? undefined} target="_blank" rel="noreferrer" dir="ltr" className="block truncate text-sm text-muted hover:underline">
                        {source.url}
                      </a>
                    </div>
                    <div className="flex flex-wrap gap-1.5">
                      <Badge tone={kindTones[source.kind ?? 'Competitor']}>{t(`competitors.kinds.${source.kind ?? 'Competitor'}`)}</Badge>
                      <StatusBadge status={source.status} />
                    </div>
                  </div>
                  {source.notes && <p className="text-sm text-muted [overflow-wrap:anywhere]">{source.notes}</p>}
                  <p className="text-sm text-muted">
                    {t('competitors.postsCount', { count: source.posts ?? 0 })}
                    {' · '}
                    {source.lastScannedAt ? t('competitors.lastScan', { when: formatRelative(source.lastScannedAt, i18n.language) }) : t('competitors.neverScanned')}
                  </p>
                  <div className="flex flex-wrap gap-2 pt-1">
                    {(source.posts ?? 0) > 0 && (
                      <Button variant="soft" size="sm" onClick={() => setViewing(source)}>{t('competitors.viewPosts')}</Button>
                    )}
                    {canEdit && (
                      <>
                        {active && (
                          <Button variant="secondary" size="sm" disabled={scanning} onClick={() => runScan(source)}>
                            {scanning ? <Spinner /> : <RefreshCw aria-hidden />}
                            {t('competitors.scan')}
                          </Button>
                        )}
                        <Button variant="ghost" size="sm" onClick={() => toggle(source)}>
                          {active ? <Pause aria-hidden /> : <Play aria-hidden />}
                          {active ? t('competitors.pause') : t('competitors.resume')}
                        </Button>
                        <Button variant="ghost" size="sm" onClick={() => setDeleting(source)} aria-label={t('competitors.deleteNamed', { name })}>
                          <Trash2 aria-hidden />
                        </Button>
                      </>
                    )}
                  </div>
                </Card>
              </li>
            )
          })}
        </ul>
      )}

      <section aria-labelledby="insights-title" className="mt-10">
        <h2 id="insights-title" className="mb-1 text-xl font-bold">{t('competitors.insightsTitle')}</h2>
        <p className="mb-4 text-muted">{t('competitors.insightsSubtitle')}</p>
        {insights.isPending ? (
          <SectionSpinner />
        ) : insights.isError ? (
          <ErrorState error={insights.error} onRetry={() => void insights.refetch()} />
        ) : (insights.data ?? []).length === 0 ? (
          <EmptyState icon={Lightbulb} title={t('competitors.noInsights')} hint={hasPosts ? t('competitors.noInsightsReady') : t('competitors.noInsightsHint')} />
        ) : (
          <ul className="grid gap-4 lg:grid-cols-2">
            {insights.data.map((insight) => (
              <li key={insight.id}>
                <Card className="h-full space-y-3">
                  <div className="flex flex-wrap items-center gap-2">
                    <Badge tone="green">{t(`competitors.insightTypes.${insight.type ?? 'Engagement'}`)}</Badge>
                    {insight.trackedSourceId && names.get(insight.trackedSourceId) && <span className="text-sm text-muted">{names.get(insight.trackedSourceId)}</span>}
                    {insight.confidence != null && (
                      <span className="ms-auto text-xs text-muted">{t('competitors.confidence', { value: formatPercent(insight.confidence, i18n.language, 0) })}</span>
                    )}
                  </div>
                  <p dir="auto" className="font-semibold [overflow-wrap:anywhere]">{insight.summary}</p>
                  {insight.applyToUs && (
                    <div className="rounded-xl bg-brand-100/60 p-3 text-sm dark:bg-brand-900/40">
                      <p className="mb-0.5 font-semibold text-brand-800 dark:text-brand-200">{t('competitors.applyToUs')}</p>
                      <p dir="auto" className="[overflow-wrap:anywhere]">{insight.applyToUs}</p>
                    </div>
                  )}
                </Card>
              </li>
            ))}
          </ul>
        )}
      </section>

      <SourceFormDialog open={adding} onClose={() => setAdding(false)} />
      <SourcePostsDialog source={viewing} onClose={() => setViewing(null)} />
      <ConfirmDialog
        open={!!deleting}
        danger
        busy={remove.isPending}
        title={t('competitors.deleteTitle', { name: deleting?.displayName || deleting?.handle || deleting?.url || '' })}
        description={t('competitors.deleteHint')}
        confirmLabel={t('common.remove')}
        onConfirm={confirmDelete}
        onClose={() => setDeleting(null)}
      />
    </>
  )
}
