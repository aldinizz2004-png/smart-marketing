import {
  ArrowLeft, CalendarClock, CheckCircle2, Circle, FileText, Hourglass, MessageCircleWarning, MessagesSquare, Sparkles, type LucideIcon,
} from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { useSession } from '@/auth/use-session'
import { buttonVariants } from '@/components/ui/button-variants'
import { Card } from '@/components/ui/card'
import { useAttentionCounts, useLatestReport, useSetupProgress, useUpcomingJobs, useWeekPerformance } from '@/features/dashboard/api'
import { usePlatformName } from '@/features/platforms/api'
import { useWorkspace } from '@/features/workspace/workspace-context'
import { cn } from '@/lib/cn'
import { formatDate, formatDateTime, formatNumber, formatPercent } from '@/lib/format'

export function HomePage() {
  const { t } = useTranslation()
  const session = useSession()
  const { business } = useWorkspace()
  const firstName = (session.user.fullName ?? '').trim().split(/\s+/)[0]

  return (
    <div className="space-y-6">
      <section className="relative overflow-hidden rounded-3xl bg-linear-to-br from-brand-800 via-brand-600 to-brand-400 p-6 text-white shadow-xl shadow-brand-700/25 md:p-10">
        <div className="pointer-events-none absolute -top-16 -end-16 size-56 rounded-full bg-lime-accent/40 blur-3xl" aria-hidden />
        <div className="pointer-events-none absolute -bottom-20 start-1/3 size-56 rounded-full bg-sun-400/30 blur-3xl" aria-hidden />
        <div className="relative max-w-xl">
          <p className="mb-3 inline-flex rounded-full bg-white/15 px-3 py-1 text-sm font-semibold backdrop-blur">{business.name}</p>
          <h1 className="text-3xl font-bold md:text-4xl">{firstName ? t('home.greetingName', { name: firstName }) : t('home.greeting')}</h1>
          <p className="mt-3 text-lg text-brand-50/90">{t('home.intro')}</p>
          <Link to="/content/new" className={cn(buttonVariants({ variant: 'sun', size: 'lg' }), 'mt-6')}>
            <Sparkles aria-hidden />
            {t('actions.createPost')}
          </Link>
        </div>
      </section>

      <SetupChecklist />
      <AttentionTiles />

      <div className="grid gap-6 lg:grid-cols-[1.4fr_1fr]">
        <WeekPerformance />
        <UpcomingPosts />
      </div>
      <LatestReport />
    </div>
  )
}

type Tile = { key: string; icon: LucideIcon; tone: string; to: string; value?: number }

function AttentionTiles() {
  const { t, i18n } = useTranslation()
  const { businessId } = useWorkspace()
  const counts = useAttentionCounts(businessId)
  const tiles: Tile[] = [
    { key: 'awaitingReview', icon: Hourglass, tone: 'from-sun-400 to-sun-500', to: '/content?status=InReview', value: counts.data?.awaitingReview },
    { key: 'complaints', icon: MessageCircleWarning, tone: 'from-coral-400 to-coral-500', to: '/comments?category=Complaint&needsReply=true', value: counts.data?.complaints },
    { key: 'conversations', icon: MessagesSquare, tone: 'from-grape-400 to-grape-500', to: '/inbox?status=WaitingForOwner', value: counts.data?.conversationsWaiting },
    { key: 'scheduled', icon: CalendarClock, tone: 'from-brand-500 to-brand-700', to: '/content?tab=schedule', value: counts.data?.scheduled },
  ]
  return (
    <section aria-label={t('home.attention')} className="grid grid-cols-2 gap-3 md:grid-cols-4 md:gap-4">
      {tiles.map(({ key, icon: Icon, tone, to, value }) => (
        <Link key={key} to={to} className="group rounded-2xl border border-line bg-surface p-5 shadow-sm transition-all hover:-translate-y-0.5 hover:shadow-md">
          <span className={cn('grid size-11 place-items-center rounded-xl bg-linear-to-br text-white shadow-md', tone)}>
            <Icon className="size-5" aria-hidden />
          </span>
          <p className="mt-3 text-3xl font-bold">{counts.isPending ? '…' : counts.isError ? '—' : formatNumber(value, i18n.language)}</p>
          <p className="text-sm text-muted">{t(`home.stats.${key}`)}</p>
        </Link>
      ))}
    </section>
  )
}

function WeekPerformance() {
  const { t, i18n } = useTranslation()
  const { businessId } = useWorkspace()
  const performance = useWeekPerformance(businessId)
  const data = performance.data
  const items = [
    { label: t('home.week.posts'), value: formatNumber(data?.posts, i18n.language) },
    { label: t('home.week.reach'), value: formatNumber(data?.totals?.reach, i18n.language) },
    { label: t('home.week.engagement'), value: formatPercent(data?.totals?.engagementRate, i18n.language) },
  ]
  return (
    <Card className="p-6">
      <div className="flex items-center justify-between gap-3">
        <h2 className="text-lg font-bold">{t('home.week.title')}</h2>
        <Link to="/insights" className="flex items-center gap-1 text-sm font-semibold text-brand-600 hover:underline dark:text-brand-400">
          {t('home.more')}
          <ArrowLeft className="size-4 ltr:rotate-180" aria-hidden />
        </Link>
      </div>
      <dl className="mt-5 grid grid-cols-3 gap-3">
        {items.map((item) => (
          <div key={item.label} className="rounded-2xl bg-surface-2 p-4">
            <dt className="text-xs text-muted">{item.label}</dt>
            <dd className="mt-1 text-2xl font-bold">{performance.isPending ? '…' : item.value}</dd>
          </div>
        ))}
      </dl>
      {data && (data.posts ?? 0) === 0 && <p className="mt-4 text-sm text-muted">{t('home.week.empty')}</p>}
      {data?.byPlatform && data.byPlatform.length > 0 && (
        <div className="mt-5 space-y-2">
          {data.byPlatform.slice(0, 4).map((group) => {
            const max = Math.max(...data.byPlatform!.map((g) => g.averageReach ?? 0), 1)
            return <PlatformBar key={group.key} platform={group.key ?? ''} reach={group.averageReach ?? 0} max={max} />
          })}
        </div>
      )}
    </Card>
  )
}

function PlatformBar({ platform, reach, max }: { platform: string; reach: number; max: number }) {
  const { t, i18n } = useTranslation()
  const platformName = usePlatformName()
  return (
    <div className="flex items-center gap-3 text-sm">
      <span className="w-24 shrink-0 truncate">{platformName(platform)}</span>
      <span className="h-2.5 flex-1 overflow-hidden rounded-full bg-surface-2">
        <span className="block h-full rounded-full bg-linear-to-r from-brand-500 to-lime-accent" style={{ width: `${Math.max(4, (reach / max) * 100)}%` }} />
      </span>
      <span className="w-28 shrink-0 text-end text-muted">{t('home.week.avgReach', { value: formatNumber(reach, i18n.language) })}</span>
    </div>
  )
}

function UpcomingPosts() {
  const { t, i18n } = useTranslation()
  const { businessId, business } = useWorkspace()
  const jobs = useUpcomingJobs(businessId)
  const platformName = usePlatformName()
  return (
    <Card className="p-6">
      <h2 className="text-lg font-bold">{t('home.upcoming.title')}</h2>
      {jobs.isPending ? (
        <p className="mt-4 text-sm text-muted">…</p>
      ) : (jobs.data ?? []).length === 0 ? (
        <div className="mt-4 rounded-2xl border-2 border-dashed border-line p-5 text-center text-sm text-muted">
          {t('home.upcoming.empty')}
          <Link to="/content/new" className="mt-2 block font-semibold text-brand-600 dark:text-brand-400">{t('actions.createPost')}</Link>
        </div>
      ) : (
        <ul className="mt-4 space-y-2">
          {jobs.data!.map((job) => (
            <li key={job.id}>
              <Link to={`/content/${job.contentItemId}`} className="flex items-center gap-3 rounded-xl p-2 hover:bg-surface-2">
                <span className="grid size-10 shrink-0 place-items-center rounded-xl bg-brand-100 text-brand-700 dark:bg-brand-900 dark:text-brand-300">
                  <CalendarClock className="size-5" aria-hidden />
                </span>
                <span className="min-w-0 flex-1">
                  <span className="block font-semibold">{platformName(job.platformCode)}</span>
                  <span className="block text-sm text-muted">{formatDateTime(job.scheduledAt, i18n.language, business.timeZoneId ?? undefined)}</span>
                </span>
              </Link>
            </li>
          ))}
        </ul>
      )}
    </Card>
  )
}

function LatestReport() {
  const { t, i18n } = useTranslation()
  const { businessId } = useWorkspace()
  const report = useLatestReport(businessId)
  if (!report.data) return null
  return (
    <Link to={`/reports/${report.data.id}`} className="flex items-start gap-4 rounded-3xl border border-line bg-surface p-6 shadow-sm hover:shadow-md">
      <span className="grid size-12 shrink-0 place-items-center rounded-2xl bg-linear-to-br from-grape-400 to-coral-400 text-white">
        <FileText className="size-6" aria-hidden />
      </span>
      <div className="min-w-0">
        <p className="text-sm text-muted">{t('home.report', { date: formatDate(report.data.periodEnd, i18n.language, { day: 'numeric', month: 'long' }) })}</p>
        <p className="mt-1 text-lg font-bold">{report.data.headline}</p>
      </div>
    </Link>
  )
}

function SetupChecklist() {
  const { t } = useTranslation()
  const { businessId } = useWorkspace()
  const setup = useSetupProgress(businessId)
  if (!setup.data || (setup.data.brand && setup.data.account && setup.data.product)) return null
  const steps = [
    { done: setup.data.brand, label: t('home.setup.brand'), to: '/settings/brand' },
    { done: setup.data.account, label: t('home.setup.account'), to: '/settings/accounts' },
    { done: setup.data.product, label: t('home.setup.product'), to: '/catalog' },
  ]
  return (
    <Card className="border-sun-400/50 bg-sun-300/10 p-6">
      <h2 className="text-lg font-bold">{t('home.setup.title')}</h2>
      <p className="mt-1 text-sm text-muted">{t('home.setup.subtitle')}</p>
      <ul className="mt-4 grid gap-2 sm:grid-cols-3">
        {steps.map((step) => (
          <li key={step.to}>
            <Link
              to={step.to}
              className={cn('flex items-center gap-2 rounded-xl border p-3 text-sm font-semibold', step.done ? 'border-brand-400/40 text-muted line-through' : 'border-line bg-surface hover:bg-surface-2')}
            >
              {step.done ? <CheckCircle2 className="size-5 text-brand-500" aria-hidden /> : <Circle className="size-5 text-sun-500" aria-hidden />}
              {step.label}
            </Link>
          </li>
        ))}
      </ul>
    </Card>
  )
}
