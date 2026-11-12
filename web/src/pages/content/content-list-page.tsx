import { CalendarClock, Images, Plus, Sparkles, X } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link, useSearchParams } from 'react-router'
import { can } from '@/auth/roles'
import { useSession } from '@/auth/use-session'
import { EmptyState } from '@/components/empty-state'
import { ErrorState } from '@/components/error-state'
import { FilterChips } from '@/components/filter-chips'
import { PageHeader } from '@/components/page-header'
import { Pagination } from '@/components/pagination'
import { StatusBadge } from '@/components/status-badge'
import { Button } from '@/components/ui/button'
import { buttonVariants } from '@/components/ui/button-variants'
import { SectionSpinner } from '@/components/ui/spinner'
import { useToast } from '@/components/ui/toast-context'
import { useCampaigns } from '@/features/campaigns/api'
import { contentStatuses, useCancelJob, useContentItems, usePublicationJobs, type ContentItem } from '@/features/content/api'
import { enumLabel } from '@/features/content/labels'
import { MediaImage } from '@/features/media/media-image'
import { usePlatformName } from '@/features/platforms/api'
import { useWorkspace } from '@/features/workspace/workspace-context'
import { cn } from '@/lib/cn'
import { formatDateTime, formatRelative } from '@/lib/format'

export function ContentListPage() {
  const { t } = useTranslation()
  const session = useSession()
  const [params, setParams] = useSearchParams()
  const tab = params.get('tab') === 'schedule' ? 'schedule' : 'posts'

  return (
    <>
      <PageHeader
        title={t('content.title')}
        subtitle={t('content.subtitle')}
        actions={
          can(session.tenant.role, 'edit') && (
            <Link to="/content/new" className={buttonVariants({ variant: 'sun' })}>
              <Sparkles aria-hidden />
              {t('actions.createPost')}
            </Link>
          )
        }
      />
      <div role="tablist" aria-label={t('content.title')} className="mb-5 flex gap-1 rounded-2xl bg-surface-2 p-1 sm:w-fit">
        {(['posts', 'schedule'] as const).map((value) => (
          <button
            key={value}
            role="tab"
            type="button"
            aria-selected={tab === value}
            onClick={() => setParams(value === 'posts' ? {} : { tab: value })}
            className={cn('flex flex-1 items-center justify-center gap-2 rounded-xl px-4 py-2 text-sm font-semibold sm:flex-none', tab === value ? 'bg-surface shadow' : 'text-muted')}
          >
            {value === 'posts' ? <Images className="size-4" aria-hidden /> : <CalendarClock className="size-4" aria-hidden />}
            {t(`content.tabs.${value}`)}
          </button>
        ))}
      </div>
      {tab === 'posts' ? <PostsTab /> : <ScheduleTab />}
    </>
  )
}

function PostsTab() {
  const { t } = useTranslation()
  const { businessId } = useWorkspace()
  const [params, setParams] = useSearchParams()
  const status = params.get('status') ?? ''
  const campaignId = params.get('campaign') ?? ''
  const page = Number(params.get('page') ?? 1)
  const items = useContentItems(businessId, { status, campaignId, page })
  const campaigns = useCampaigns(businessId)

  const update = (key: string, value: string) => {
    const next = new URLSearchParams(params)
    if (value) next.set(key, value)
    else next.delete(key)
    if (key !== 'page') next.delete('page')
    setParams(next)
  }

  const campaign = campaigns.data?.items?.find((c) => c.id === campaignId)

  return (
    <div className="space-y-5">
      <FilterChips
        label={t('content.filterStatus')}
        value={status}
        onChange={(v) => update('status', v)}
        options={[{ value: '', label: t('common.all') }, ...contentStatuses.map((s) => ({ value: s, label: t(`content.statuses.${s}`) }))]}
      />
      {campaign && (
        <p className="flex items-center gap-2 text-sm">
          {t('content.inCampaign', { name: campaign.name })}
          <button type="button" onClick={() => update('campaign', '')} className="rounded-full p-1 hover:bg-surface-2" aria-label={t('common.clearFilter')}>
            <X className="size-4" aria-hidden />
          </button>
        </p>
      )}
      {items.isPending ? (
        <SectionSpinner />
      ) : items.isError ? (
        <ErrorState error={items.error} onRetry={() => void items.refetch()} />
      ) : (items.data.items ?? []).length === 0 ? (
        <EmptyState
          icon={Sparkles}
          title={status ? t('content.emptyFiltered') : t('content.empty')}
          hint={t('content.emptyHint')}
          action={<Link to="/content/new" className={buttonVariants({ variant: 'sun' })}><Plus aria-hidden />{t('actions.createPost')}</Link>}
        />
      ) : (
        <>
          <ul className={cn('grid gap-4 sm:grid-cols-2 xl:grid-cols-3', items.isFetching && 'opacity-70')}>
            {items.data.items!.map((item) => <PostCard key={item.id} item={item} />)}
          </ul>
          <Pagination page={page} totalPages={items.data.totalPages ?? 1} onChange={(p) => update('page', String(p))} />
        </>
      )}
    </div>
  )
}

function PostCard({ item }: { item: ContentItem }) {
  const { t, i18n } = useTranslation()
  const platformName = usePlatformName()
  const revision = item.latestRevision
  const firstAsset = revision?.assets?.[0]
  return (
    <li>
      <Link to={`/content/${item.id}`} className="flex h-full flex-col overflow-hidden rounded-2xl border border-line bg-surface shadow-sm transition-all hover:-translate-y-0.5 hover:shadow-md">
        {firstAsset ? (
          <MediaImage contentUrl={firstAsset.contentUrl} mimeType={firstAsset.mimeType} alt={t('content.postImage')} className="aspect-video w-full" />
        ) : (
          <div className="grid aspect-video place-items-center bg-linear-to-br from-brand-100 via-sun-300/20 to-grape-400/20 dark:from-brand-900 dark:via-brand-800 dark:to-grape-500/20">
            <Sparkles className="size-8 text-brand-500" aria-hidden />
          </div>
        )}
        <div className="flex flex-1 flex-col gap-3 p-4">
          <div className="flex items-center justify-between gap-2">
            <span className="text-sm font-semibold">{platformName(item.platformCode)} · {enumLabel(t, 'contentType', item.contentType)}</span>
            <StatusBadge status={item.status} />
          </div>
          <p className="line-clamp-3 flex-1 text-sm text-muted [overflow-wrap:anywhere]" dir="auto">{revision?.body || t('content.noText')}</p>
          <p className="text-xs text-muted">
            {item.plannedPublishAt ? t('content.plannedFor', { time: formatDateTime(item.plannedPublishAt, i18n.language) }) : formatRelative(item.createdAt, i18n.language)}
          </p>
        </div>
      </Link>
    </li>
  )
}

const jobFilters = ['', 'Queued', 'Running', 'Succeeded', 'Failed', 'Canceled'] as const

function ScheduleTab() {
  const { t, i18n } = useTranslation()
  const toast = useToast()
  const session = useSession()
  const { businessId, business } = useWorkspace()
  const [params, setParams] = useSearchParams()
  const status = params.get('status') ?? ''
  const jobs = usePublicationJobs(businessId, status)
  const cancel = useCancelJob(businessId)
  const platformName = usePlatformName()

  const sorted = [...(jobs.data?.items ?? [])].sort((a, b) => Date.parse(b.scheduledAt ?? '') - Date.parse(a.scheduledAt ?? ''))

  return (
    <div className="space-y-5">
      <FilterChips
        label={t('content.filterStatus')}
        value={status}
        onChange={(v) => setParams(v ? { tab: 'schedule', status: v } : { tab: 'schedule' })}
        options={jobFilters.map((s) => ({ value: s, label: s ? t(`content.jobStatuses.${s}`) : t('common.all') }))}
      />
      {jobs.isPending ? (
        <SectionSpinner />
      ) : jobs.isError ? (
        <ErrorState error={jobs.error} onRetry={() => void jobs.refetch()} />
      ) : sorted.length === 0 ? (
        <EmptyState icon={CalendarClock} title={t('content.noJobs')} hint={t('content.noJobsHint')} />
      ) : (
        <ul className="divide-y divide-line overflow-hidden rounded-2xl border border-line bg-surface">
          {sorted.map((job) => (
            <li key={job.id} className="flex flex-wrap items-center gap-3 p-4">
              <span className="grid size-11 shrink-0 place-items-center rounded-xl bg-brand-100 text-brand-700 dark:bg-brand-900 dark:text-brand-300">
                <CalendarClock className="size-5" aria-hidden />
              </span>
              <div className="min-w-0 flex-1">
                <Link to={`/content/${job.contentItemId}`} className="font-semibold hover:underline">{platformName(job.platformCode)}</Link>
                <p className="text-sm text-muted">{formatDateTime(job.scheduledAt, i18n.language, business.timeZoneId ?? undefined)}</p>
                {job.lastError && <p className="mt-1 text-xs text-coral-500 [overflow-wrap:anywhere]">{job.lastError}</p>}
              </div>
              {job.publication?.externalUrl && (
                <a href={job.publication.externalUrl} target="_blank" rel="noreferrer" className="text-sm font-semibold text-brand-600 hover:underline dark:text-brand-400">
                  {t('content.viewPost')}
                </a>
              )}
              <StatusBadge status={job.status} />
              {can(session.tenant.role, 'edit') && ['Queued', 'Retry'].includes(job.status?.code ?? '') && (
                <Button
                  variant="ghost"
                  size="sm"
                  disabled={cancel.isPending}
                  onClick={() => cancel.mutate(job.id ?? '', { onSuccess: () => toast.success(t('content.jobCanceled')), onError: (e) => toast.error(e) })}
                >
                  {t('common.cancel')}
                </Button>
              )}
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
