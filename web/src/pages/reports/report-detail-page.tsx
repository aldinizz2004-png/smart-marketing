import { ArrowRight, CircleAlert, CircleCheck, Eye, ListChecks, MailCheck, Send, TrendingDown, TrendingUp } from 'lucide-react'
import { useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router'
import { can } from '@/auth/roles'
import { useSession } from '@/auth/use-session'
import { ConfirmDialog } from '@/components/confirm-dialog'
import { ErrorState } from '@/components/error-state'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { SectionSpinner } from '@/components/ui/spinner'
import { useToast } from '@/components/ui/toast-context'
import { usePlatformName } from '@/features/platforms/api'
import { changeRatio, useReport, useSendReport } from '@/features/reports/api'
import { EmailPreviewDialog } from '@/features/reports/email-preview-dialog'
import { useWorkspace } from '@/features/workspace/workspace-context'
import { formatDate, formatDateTime, formatNumber, formatPercent } from '@/lib/format'

function Change({ current, previous }: { current?: number; previous?: number }) {
  const { t, i18n } = useTranslation()
  const ratio = changeRatio(current, previous)
  if (ratio == null) return <span className="text-xs text-muted">{t('reports.noPrevious')}</span>
  const up = ratio >= 0
  const Icon = up ? TrendingUp : TrendingDown
  return (
    <span className={up ? 'inline-flex items-center gap-1 text-xs font-semibold text-brand-700 dark:text-brand-300' : 'inline-flex items-center gap-1 text-xs font-semibold text-coral-500'}>
      <Icon className="size-3.5" aria-hidden />
      <span dir="ltr">{up ? '+' : ''}{formatPercent(ratio, i18n.language, 0)}</span>
      <span className="sr-only">{up ? t('reports.up') : t('reports.down')}</span>
    </span>
  )
}

function Stat({ label, value, children }: { label: string; value: string; children?: ReactNode }) {
  return (
    <Card className="p-4">
      <dt className="text-sm text-muted">{label}</dt>
      <dd className="mt-1 text-2xl font-bold">{value}</dd>
      {children && <div className="mt-1">{children}</div>}
    </Card>
  )
}

function Bullets({ title, items, tone, icon: Icon }: { title: string; items: string[]; tone: string; icon: typeof CircleCheck }) {
  if (items.length === 0) return null
  return (
    <Card className="h-full">
      <h2 className={`mb-3 flex items-center gap-2 font-bold ${tone}`}>
        <Icon className="size-5" aria-hidden />
        {title}
      </h2>
      <ul className="space-y-2">
        {items.map((item, i) => <li key={i} dir="auto" className="[overflow-wrap:anywhere]">{item}</li>)}
      </ul>
    </Card>
  )
}

export function ReportDetailPage() {
  const { id = '' } = useParams()
  const { t, i18n } = useTranslation()
  const toast = useToast()
  const session = useSession()
  const { businessId, business } = useWorkspace()
  const report = useReport(businessId, id)
  const send = useSendReport(businessId)
  const platformName = usePlatformName()
  const [previewing, setPreviewing] = useState(false)
  const [confirmSend, setConfirmSend] = useState(false)
  const canSend = can(session.tenant.role, 'admin')

  if (report.isPending) return <SectionSpinner />
  if (report.isError) return <ErrorState error={report.error} onRetry={() => void report.refetch()} />

  const r = report.data
  const f = r.facts ?? {}
  const categories = Object.entries(f.commentsByCategory ?? {}).filter(([, count]) => count > 0)
  const groupLabel = (kind: 'format' | 'hook', key?: string | null) => (key && key !== 'None' ? t(`enums.${kind}.${key}`, { defaultValue: key }) : '—')

  const sendNow = () =>
    send.mutate(id, {
      onSuccess: (sent) => {
        toast.success(t('reports.sent', { to: sent.sentTo ?? '' }))
        setConfirmSend(false)
      },
      onError: (e) => {
        toast.error(e)
        setConfirmSend(false)
      },
    })

  return (
    <>
      <Link to="/reports" className="mb-4 inline-flex items-center gap-1 text-sm font-semibold text-muted hover:text-ink">
        <ArrowRight className="size-4 ltr:rotate-180" aria-hidden />
        {t('reports.back')}
      </Link>
      <header className="mb-6 flex flex-wrap items-start justify-between gap-4">
        <div className="min-w-0">
          <p className="mb-1 text-sm text-muted">{t('reports.periodRange', { from: formatDate(r.periodStart, i18n.language), to: formatDate(r.periodEnd, i18n.language) })}</p>
          <h1 dir="auto" className="text-2xl font-bold tracking-tight [overflow-wrap:anywhere] md:text-3xl">{r.headline}</h1>
          <div className="mt-3 flex flex-wrap gap-1.5">
            <Badge tone={r.isScheduled ? 'grape' : 'gray'}>{r.isScheduled ? t('reports.scheduled') : t('reports.manual')}</Badge>
            {r.sentAt ? (
              <Badge tone="green"><MailCheck className="size-3.5" aria-hidden />{t('reports.sentAt', { when: formatDateTime(r.sentAt, i18n.language, business.timeZoneId ?? undefined) })}</Badge>
            ) : (
              <Badge tone="sun">{t('reports.notSent')}</Badge>
            )}
          </div>
          {r.sentTo && <p className="mt-2 text-sm text-muted [overflow-wrap:anywhere]">{t('reports.sentTo', { to: r.sentTo })}</p>}
        </div>
        <div className="flex flex-wrap gap-2">
          <Button variant="secondary" onClick={() => setPreviewing(true)}>
            <Eye aria-hidden />
            {t('reports.viewEmail')}
          </Button>
          {canSend && (
            <Button onClick={() => setConfirmSend(true)}>
              <Send aria-hidden />
              {r.sentAt ? t('reports.sendAgain') : t('reports.send')}
            </Button>
          )}
        </div>
      </header>

      {r.summary && (
        <Card className="mb-6">
          <p dir="auto" className="text-lg leading-relaxed [overflow-wrap:anywhere]">{r.summary}</p>
        </Card>
      )}

      <div className="mb-6 grid gap-4 lg:grid-cols-3">
        <Bullets title={t('reports.wins')} items={r.wins ?? []} tone="text-brand-700 dark:text-brand-300" icon={CircleCheck} />
        <Bullets title={t('reports.concerns')} items={r.concerns ?? []} tone="text-coral-500" icon={CircleAlert} />
        <Bullets title={t('reports.nextActions')} items={r.nextActions ?? []} tone="text-grape-500" icon={ListChecks} />
      </div>

      <section aria-labelledby="facts-title" className="mb-6">
        <h2 id="facts-title" className="mb-3 text-xl font-bold">{t('reports.numbers')}</h2>
        <dl className="grid grid-cols-2 gap-3 lg:grid-cols-4">
          <Stat label={t('reports.posts')} value={formatNumber(f.posts, i18n.language)}><Change current={f.posts} previous={f.previousPosts} /></Stat>
          <Stat label={t('reports.reach')} value={formatNumber(f.reach, i18n.language)}><Change current={f.reach} previous={f.previousReach} /></Stat>
          <Stat label={t('reports.engagement')} value={formatPercent(f.engagementRate, i18n.language)}><Change current={f.engagementRate} previous={f.previousEngagementRate} /></Stat>
          <Stat label={t('reports.newComments')} value={formatNumber(f.newComments, i18n.language)} />
        </dl>
      </section>

      <div className="mb-6 grid gap-4 lg:grid-cols-2">
        <Card>
          <h2 className="mb-3 font-bold">{t('reports.best')}</h2>
          <dl className="space-y-2 text-sm">
            {[
              [t('reports.bestPlatform'), f.bestPlatform?.key ? platformName(f.bestPlatform.key) : '—', f.bestPlatform],
              [t('reports.bestFormat'), groupLabel('format', f.bestFormat?.key), f.bestFormat],
              [t('reports.bestHook'), groupLabel('hook', f.bestHook?.key), f.bestHook],
            ].map(([label, value, group]) => (
              <div key={String(label)} className="flex flex-wrap items-baseline justify-between gap-2">
                <dt className="text-muted">{String(label)}</dt>
                <dd className="font-semibold">
                  {String(value)}
                  {(group as { engagementRate?: number } | undefined)?.engagementRate != null && value !== '—' && (
                    <span className="ms-2 font-normal text-muted">{formatPercent((group as { engagementRate: number }).engagementRate, i18n.language)}</span>
                  )}
                </dd>
              </div>
            ))}
          </dl>
        </Card>
        <Card>
          <h2 className="mb-3 font-bold">{t('reports.topPost')}</h2>
          {f.topPost ? (
            <div className="space-y-2">
              <div className="flex flex-wrap gap-1.5">
                {f.topPost.platform && <Badge tone="green">{platformName(f.topPost.platform)}</Badge>}
                {f.topPost.format && <Badge tone="grape">{groupLabel('format', f.topPost.format)}</Badge>}
                {f.topPost.hook && f.topPost.hook !== 'None' && <Badge tone="sun">{groupLabel('hook', f.topPost.hook)}</Badge>}
              </div>
              {f.topPost.excerpt && <p dir="auto" className="line-clamp-3 [overflow-wrap:anywhere]">{f.topPost.excerpt}</p>}
              <p className="text-sm text-muted">{t('reports.topLine', { reach: formatNumber(f.topPost.reach, i18n.language), rate: formatPercent(f.topPost.engagementRate, i18n.language) })}</p>
              {f.topPost.url && <a href={f.topPost.url} target="_blank" rel="noreferrer" className="text-sm font-semibold text-brand-700 hover:underline dark:text-brand-300">{t('reports.openPost')}</a>}
            </div>
          ) : (
            <p className="text-muted">{t('reports.noTopPost')}</p>
          )}
        </Card>
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        <Card>
          <h2 className="mb-3 font-bold">{t('reports.attention')}</h2>
          <ul className="space-y-2 text-sm">
            <li className="flex justify-between gap-3"><span>{t('reports.unansweredComplaints')}</span><strong>{f.unansweredComplaints ?? 0}</strong></li>
            <li className="flex justify-between gap-3"><span>{t('reports.unansweredQuestions')}</span><strong>{f.unansweredQuestions ?? 0}</strong></li>
            <li className="flex justify-between gap-3"><span>{t('reports.awaitingReview')}</span><strong>{f.awaitingReview ?? 0}</strong></li>
            <li className="flex justify-between gap-3"><span>{t('reports.approvedNotScheduled')}</span><strong>{f.approvedNotScheduled ?? 0}</strong></li>
            <li className="flex justify-between gap-3"><span>{t('reports.scheduledNext7Days')}</span><strong>{f.scheduledNext7Days ?? 0}</strong></li>
          </ul>
          {categories.length > 0 && (
            <p className="mt-3 flex flex-wrap gap-1.5">
              {categories.map(([category, count]) => (
                <Badge key={category}>{t(`comments.categories.${category}`, { defaultValue: category })}: {count}</Badge>
              ))}
            </p>
          )}
        </Card>
        {(f.offersEndingSoon ?? []).length > 0 && (
          <Card>
            <h2 className="mb-3 font-bold">{t('reports.offersEnding')}</h2>
            <ul className="list-disc space-y-1 ps-5">
              {f.offersEndingSoon!.map((offer, i) => <li key={i} dir="auto" className="[overflow-wrap:anywhere]">{offer}</li>)}
            </ul>
          </Card>
        )}
      </div>

      <EmailPreviewDialog reportId={id} open={previewing} onClose={() => setPreviewing(false)} />
      <ConfirmDialog
        open={confirmSend}
        busy={send.isPending}
        title={r.sentAt ? t('reports.sendAgainTitle') : t('reports.sendTitle')}
        description={t('reports.sendHint')}
        confirmLabel={send.isPending ? t('reports.sending') : t('reports.send')}
        onConfirm={sendNow}
        onClose={() => setConfirmSend(false)}
      />
    </>
  )
}
