import { ChartNoAxesColumn, ExternalLink } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { EmptyState } from '@/components/empty-state'
import { ErrorState } from '@/components/error-state'
import { FilterChips } from '@/components/filter-chips'
import { Badge } from '@/components/ui/badge'
import { Card } from '@/components/ui/card'
import { SectionSpinner } from '@/components/ui/spinner'
import { usePlatforms } from '@/features/platforms/api'
import { rangeDays, usePerformance, type PerformanceGroup } from '@/features/insights/api'
import { useWorkspace } from '@/features/workspace/workspace-context'
import { cn } from '@/lib/cn'
import { formatNumber, formatPercent } from '@/lib/format'

type GroupKind = 'platform' | 'format' | 'hook'

function Breakdown({ kind, groups }: { kind: GroupKind; groups: PerformanceGroup[] }) {
  const { t, i18n } = useTranslation()
  const platforms = usePlatforms()
  const best = Math.max(...groups.map((g) => g.engagementRate ?? 0), 0)
  const label = (key: string) => {
    if (key === 'None') return t('insights.performance.none')
    if (kind === 'platform') return platforms.data?.find((p) => p.code === key)?.displayName ?? key
    return t(`enums.${kind}.${key}`, { defaultValue: key })
  }
  return (
    <Card>
      <h3 className="mb-3 font-bold">{t(`insights.performance.by.${kind}`)}</h3>
      {groups.length === 0 ? (
        <p className="text-sm text-muted">{t('insights.performance.noGroups')}</p>
      ) : (
        <ul className="space-y-3">
          {groups.map((g) => {
            const rate = g.engagementRate ?? 0
            return (
              <li key={g.key}>
                <div className="mb-1 flex items-baseline justify-between gap-2 text-sm">
                  <span className="font-semibold">{label(g.key ?? '')}</span>
                  <span className="text-muted">
                    {t('insights.performance.groupLine', { posts: g.posts ?? 0, reach: formatNumber(g.averageReach, i18n.language), rate: formatPercent(rate, i18n.language) })}
                  </span>
                </div>
                <div className="h-2 overflow-hidden rounded-full bg-surface-2" aria-hidden>
                  <div className="h-full rounded-full bg-linear-to-r from-brand-600 to-lime-accent" style={{ width: `${best > 0 ? Math.max(4, (rate / best) * 100) : 0}%` }} />
                </div>
              </li>
            )
          })}
        </ul>
      )}
    </Card>
  )
}

export function PerformanceTab({ days, onDaysChange }: { days: number; onDaysChange: (days: number) => void }) {
  const { t, i18n } = useTranslation()
  const { businessId } = useWorkspace()
  const performance = usePerformance(businessId, days)
  const platforms = usePlatforms()
  const data = performance.data
  const totals = data?.totals

  const cards = [
    { label: t('insights.performance.posts'), value: formatNumber(data?.posts, i18n.language) },
    { label: t('insights.performance.reach'), value: formatNumber(totals?.reach, i18n.language) },
    { label: t('insights.performance.impressions'), value: formatNumber(totals?.impressions, i18n.language) },
    { label: t('insights.performance.engagement'), value: formatPercent(totals?.engagementRate, i18n.language) },
    { label: t('insights.performance.likes'), value: formatNumber(totals?.likes, i18n.language) },
    { label: t('insights.performance.comments'), value: formatNumber(totals?.comments, i18n.language) },
    { label: t('insights.performance.shares'), value: formatNumber(totals?.shares, i18n.language) },
    { label: t('insights.performance.saves'), value: formatNumber(totals?.saves, i18n.language) },
    { label: t('insights.performance.clicks'), value: formatNumber(totals?.linkClicks, i18n.language) },
  ]

  return (
    <>
      <div className="mb-5">
        <FilterChips
          label={t('insights.performance.range')}
          value={String(days)}
          onChange={(v) => onDaysChange(Number(v))}
          options={rangeDays.map((d) => ({ value: String(d), label: t('insights.performance.lastDays', { count: d }) }))}
        />
      </div>
      {performance.isPending ? (
        <SectionSpinner />
      ) : performance.isError ? (
        <ErrorState error={performance.error} onRetry={() => void performance.refetch()} />
      ) : (data?.posts ?? 0) === 0 ? (
        <EmptyState icon={ChartNoAxesColumn} title={t('insights.performance.empty')} hint={t('insights.performance.emptyHint')} />
      ) : (
        <div className={cn('space-y-6', performance.isFetching && 'opacity-70')}>
          <dl className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-5">
            {cards.map((c) => (
              <Card key={c.label} className="p-4">
                <dt className="text-sm text-muted">{c.label}</dt>
                <dd className="mt-1 text-2xl font-bold">{c.value}</dd>
              </Card>
            ))}
          </dl>
          <div className="grid gap-4 lg:grid-cols-3">
            <Breakdown kind="platform" groups={data?.byPlatform ?? []} />
            <Breakdown kind="format" groups={data?.byFormat ?? []} />
            <Breakdown kind="hook" groups={data?.byHook ?? []} />
          </div>
          <section aria-labelledby="top-posts-title">
            <h2 id="top-posts-title" className="mb-3 text-xl font-bold">{t('insights.performance.topPosts')}</h2>
            {(data?.topPosts ?? []).length === 0 ? (
              <p className="text-muted">{t('insights.performance.noTop')}</p>
            ) : (
              <ol className="space-y-3">
                {data!.topPosts!.map((post, index) => (
                  <li key={post.publicationId}>
                    <Card className="flex flex-wrap items-center gap-3 p-4">
                      <span className="grid size-8 place-items-center rounded-full bg-brand-100 text-sm font-bold text-brand-800 dark:bg-brand-900 dark:text-brand-200">{index + 1}</span>
                      <div className="flex flex-wrap gap-1.5">
                        <Badge tone="green">{platforms.data?.find((p) => p.code === post.platformCode)?.displayName ?? post.platformCode}</Badge>
                        {post.format && <Badge tone="grape">{t(`enums.format.${post.format}`)}</Badge>}
                        {post.hookType && <Badge tone="sun">{t(`enums.hook.${post.hookType}`)}</Badge>}
                      </div>
                      <p className="ms-auto text-sm text-muted">
                        {t('insights.performance.topLine', { reach: formatNumber(post.reach, i18n.language), rate: formatPercent(post.engagementRate, i18n.language) })}
                      </p>
                      {post.externalUrl && (
                        <a href={post.externalUrl} target="_blank" rel="noreferrer" className="inline-flex items-center gap-1 text-sm font-semibold text-brand-700 hover:underline dark:text-brand-300">
                          {t('insights.performance.openPost')}
                          <ExternalLink className="size-4" aria-hidden />
                        </a>
                      )}
                    </Card>
                  </li>
                ))}
              </ol>
            )}
          </section>
        </div>
      )}
    </>
  )
}
