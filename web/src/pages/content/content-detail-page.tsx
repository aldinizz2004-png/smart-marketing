import { ArrowRight, Ban, CalendarClock, CheckCircle2, ExternalLink, MessageSquareWarning, PenLine, RefreshCw, Send, Sparkles, Undo2, XCircle } from 'lucide-react'
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
import { Card } from '@/components/ui/card'
import { Dialog } from '@/components/ui/dialog'
import { Field } from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { SectionSpinner, Spinner } from '@/components/ui/spinner'
import { Textarea } from '@/components/ui/textarea'
import { useToast } from '@/components/ui/toast-context'
import { useCampaign } from '@/features/campaigns/api'
import {
  useAddRevision, useCancelJob, useChangeContentStatus, useContentItem, usePublicationJobs, usePublicationMetrics, useRefreshMetrics,
  useReviewContent, useScheduleContent, type ContentItem, type PublicationJob,
} from '@/features/content/api'
import { enumLabel } from '@/features/content/labels'
import { mediaProblem } from '@/features/content/media-rules'
import { PostPreview } from '@/features/content/post-preview'
import { RevisionComposer } from '@/features/content/revision-composer'
import { usePlatformName } from '@/features/platforms/api'
import { useSocialAccounts } from '@/features/social/api'
import { useWorkspace } from '@/features/workspace/workspace-context'
import { cn } from '@/lib/cn'
import { fromDateTimeInput } from '@/lib/dates'
import { formatDateTime, formatNumber } from '@/lib/format'

export function ContentDetailPage() {
  const { id = '' } = useParams()
  const { t } = useTranslation()
  const { businessId } = useWorkspace()
  const item = useContentItem(businessId, id)
  const platformName = usePlatformName()
  const [shownRevisionId, setShownRevisionId] = useState<string | null>(null)

  if (item.isPending) return <SectionSpinner />
  if (item.isError) return <ErrorState error={item.error} onRetry={() => void item.refetch()} />

  const data = item.data
  const revisions = [...(data.revisions ?? [])].sort((a, b) => (b.revisionNo ?? 0) - (a.revisionNo ?? 0))
  const shown = revisions.find((r) => r.id === shownRevisionId) ?? data.latestRevision

  return (
    <div className="space-y-6">
      <div>
        <Link to="/content" className="mb-3 inline-flex items-center gap-1 text-sm text-muted hover:text-ink">
          <ArrowRight className="size-4 ltr:rotate-180" aria-hidden />
          {t('content.back')}
        </Link>
        <div className="flex flex-wrap items-center gap-3">
          <h1 className="text-2xl font-bold md:text-3xl">
            {t('content.detailTitle', { type: enumLabel(t, 'contentType', data.contentType), platform: platformName(data.platformCode) })}
          </h1>
          <StatusBadge status={data.status} />
        </div>
        <CampaignLine campaignId={data.campaignId} />
      </div>

      <div className="grid items-start gap-6 lg:grid-cols-[minmax(0,420px)_1fr]">
        <div className="lg:sticky lg:top-20">
          <PostPreview revision={shown} platformCode={data.platformCode} />
          {shown && shown.id !== data.latestRevision?.id && (
            <p className="mt-2 text-center text-sm text-muted">{t('content.showingOld', { n: shown.revisionNo })}</p>
          )}
        </div>
        <div className="space-y-6">
          <ActionsCard item={data} />
          <PublicationCard item={data} />
          <Card className="p-6">
            <h2 className="text-lg font-bold">{t('content.history')}</h2>
            <ol className="mt-4 space-y-3">
              {revisions.map((revision) => (
                <li key={revision.id}>
                  <button
                    type="button"
                    onClick={() => setShownRevisionId(revision.id ?? null)}
                    aria-pressed={shown?.id === revision.id}
                    className={cn('w-full rounded-2xl border p-4 text-start transition-colors', shown?.id === revision.id ? 'border-brand-500 bg-brand-50/60 dark:bg-brand-900/30' : 'border-line hover:bg-surface-2')}
                  >
                    <span className="flex flex-wrap items-center gap-2">
                      <span className="font-semibold">{t('content.version', { n: revision.revisionNo })}</span>
                      <Badge tone={revision.author === 'ai' ? 'grape' : 'gray'}>
                        {revision.author === 'ai' ? <Sparkles className="size-3" aria-hidden /> : <PenLine className="size-3" aria-hidden />}
                        {revision.author === 'ai' ? t('content.byAi') : t('content.byPerson')}
                      </Badge>
                      <RevisionTime value={revision.createdAt} />
                    </span>
                    <span className="mt-1 line-clamp-2 block text-sm text-muted" dir="auto">{revision.body}</span>
                    {(revision.reviews ?? []).map((review) => (
                      <span key={review.id} className="mt-2 flex flex-wrap items-center gap-2 text-sm">
                        <StatusBadge status={review.decision} />
                        {review.feedback && <span className="text-muted" dir="auto">{review.feedback}</span>}
                      </span>
                    ))}
                  </button>
                </li>
              ))}
            </ol>
          </Card>
        </div>
      </div>
    </div>
  )
}

function RevisionTime({ value }: { value?: string }) {
  const { i18n } = useTranslation()
  return <span className="text-xs text-muted">{formatDateTime(value, i18n.language)}</span>
}

function CampaignLine({ campaignId }: { campaignId?: string }) {
  const { t } = useTranslation()
  const { businessId } = useWorkspace()
  const campaign = useCampaign(businessId, campaignId ?? '')
  if (!campaign.data) return null
  return (
    <p className="mt-1 text-sm text-muted">
      {t('content.campaign')}:{' '}
      <Link to={`/campaigns/${campaign.data.id}`} className="font-semibold text-brand-600 hover:underline dark:text-brand-400">{campaign.data.name}</Link>
    </p>
  )
}

function ActionsCard({ item }: { item: ContentItem }) {
  const { t } = useTranslation()
  const toast = useToast()
  const session = useSession()
  const { businessId } = useWorkspace()
  const id = item.id ?? ''
  const status = item.status?.code ?? ''
  const changeStatus = useChangeContentStatus(businessId, id)
  const addRevision = useAddRevision(businessId, id)
  const [dialog, setDialog] = useState<null | 'revision' | 'schedule' | 'cancel' | 'ChangesRequested' | 'Rejected'>(null)
  const canEdit = can(session.tenant.role, 'edit')
  const canReview = can(session.tenant.role, 'review')

  const move = (target: string, message: string) =>
    changeStatus.mutate(target, { onSuccess: () => toast.success(message), onError: (e) => toast.error(e) })

  if (status === 'Published' || status === 'Canceled') return null

  const latest = item.latestRevision
  const problem = status === 'Draft' ? mediaProblem(latest?.format, latest?.assets?.length ?? 0) : null

  return (
    <Card className="p-6">
      <h2 className="text-lg font-bold">{t(`content.next.${status}`)}</h2>
      <p className="mt-1 text-sm text-muted">{t(`content.next.${status}Hint`)}</p>
      {problem && canEdit && (
        <Alert tone="info" title={t(`content.media.${problem.key}`, { format: enumLabel(t, 'format', latest?.format), min: problem.min, max: problem.max })} className="mt-4">
          {t('content.media.fix')}
        </Alert>
      )}
      <div className="mt-5 flex flex-wrap gap-2">
        {status === 'Draft' && canEdit && (
          <>
            <Button onClick={() => move('InReview', t('content.sentToReview'))} disabled={changeStatus.isPending || Boolean(problem)}>
              <Send aria-hidden />
              {t('content.sendToReview')}
            </Button>
            <Button variant="secondary" onClick={() => setDialog('revision')}>
              <PenLine aria-hidden />
              {t('content.newVersion')}
            </Button>
          </>
        )}
        {status === 'InReview' && canReview && <ReviewButtons item={item} onAskFeedback={setDialog} />}
        {status === 'InReview' && canEdit && (
          <Button variant="secondary" onClick={() => move('Draft', t('content.backToDraftDone'))} disabled={changeStatus.isPending}>
            <Undo2 aria-hidden />
            {t('content.backToDraft')}
          </Button>
        )}
        {status === 'Approved' && canEdit && (
          <>
            <Button variant="sun" onClick={() => setDialog('schedule')}>
              <CalendarClock aria-hidden />
              {t('content.schedule')}
            </Button>
            <Button variant="secondary" onClick={() => move('Draft', t('content.backToDraftDone'))} disabled={changeStatus.isPending}>
              <Undo2 aria-hidden />
              {t('content.backToDraft')}
            </Button>
          </>
        )}
        {status === 'Scheduled' && <ScheduledJob contentItemId={id} />}
        {canEdit && ['Draft', 'InReview', 'Approved'].includes(status) && (
          <Button variant="ghost" onClick={() => setDialog('cancel')}>
            <Ban aria-hidden />
            {t('content.cancelPost')}
          </Button>
        )}
        {status === 'InReview' && !canReview && !canEdit && <p className="text-sm text-muted">{t('content.waitingReviewer')}</p>}
      </div>

      <Dialog open={dialog === 'revision'} onClose={() => setDialog(null)} size="lg" title={t('content.newVersion')} description={t('content.newVersionHint')}>
        <RevisionComposer
          businessId={businessId}
          base={item.latestRevision}
          submitLabel={t('content.saveVersion')}
          onSubmit={async (request) => {
            try {
              await addRevision.mutateAsync(request)
              toast.success(t('content.versionSaved'))
              setDialog(null)
            } catch (error) {
              toast.error(error)
            }
          }}
        />
      </Dialog>
      {dialog === 'schedule' && <ScheduleDialog item={item} onClose={() => setDialog(null)} />}
      {(dialog === 'ChangesRequested' || dialog === 'Rejected') && <FeedbackDialog item={item} decision={dialog} onClose={() => setDialog(null)} />}
      <ConfirmDialog
        open={dialog === 'cancel'}
        title={t('content.cancelTitle')}
        description={t('content.cancelHint')}
        confirmLabel={t('content.cancelPost')}
        danger
        busy={changeStatus.isPending}
        onClose={() => setDialog(null)}
        onConfirm={() => changeStatus.mutate('Canceled', { onSuccess: () => { setDialog(null); toast.success(t('content.canceled')) }, onError: (e) => toast.error(e) })}
      />
    </Card>
  )
}

function ReviewButtons({ item, onAskFeedback }: { item: ContentItem; onAskFeedback: (d: 'ChangesRequested' | 'Rejected') => void }) {
  const { t } = useTranslation()
  const toast = useToast()
  const { businessId } = useWorkspace()
  const review = useReviewContent(businessId, item.id ?? '')
  return (
    <>
      <Button
        onClick={() => review.mutate({ decision: 'Approved' }, { onSuccess: () => toast.success(t('content.approved')), onError: (e) => toast.error(e) })}
        disabled={review.isPending}
      >
        {review.isPending ? <Spinner /> : <CheckCircle2 aria-hidden />}
        {t('content.approve')}
      </Button>
      <Button variant="secondary" onClick={() => onAskFeedback('ChangesRequested')}>
        <MessageSquareWarning aria-hidden />
        {t('content.requestChanges')}
      </Button>
      <Button variant="ghost" onClick={() => onAskFeedback('Rejected')}>
        <XCircle aria-hidden />
        {t('content.reject')}
      </Button>
    </>
  )
}

function FeedbackDialog({ item, decision, onClose }: { item: ContentItem; decision: 'ChangesRequested' | 'Rejected'; onClose: () => void }) {
  const { t } = useTranslation()
  const toast = useToast()
  const { businessId } = useWorkspace()
  const review = useReviewContent(businessId, item.id ?? '')
  const [feedback, setFeedback] = useState('')
  const [error, setError] = useState<string | undefined>()
  const submit = () => {
    if (!feedback.trim()) {
      setError('validation.required')
      return
    }
    review.mutate(
      { decision, feedback: feedback.trim() },
      { onSuccess: () => { toast.success(t(decision === 'Rejected' ? 'content.rejected' : 'content.changesSent')); onClose() }, onError: (e) => toast.error(e) },
    )
  }
  return (
    <Dialog
      open
      onClose={onClose}
      title={decision === 'Rejected' ? t('content.reject') : t('content.requestChanges')}
      footer={
        <>
          <Button variant="ghost" onClick={onClose}>{t('common.cancel')}</Button>
          <Button variant={decision === 'Rejected' ? 'danger' : 'primary'} onClick={submit} disabled={review.isPending}>
            {review.isPending && <Spinner />}
            {t('common.send')}
          </Button>
        </>
      }
    >
      <Field label={t('content.feedback')} hint={t('content.feedbackHint')} error={error}>
        <Textarea rows={4} maxLength={2000} value={feedback} onChange={(e) => setFeedback(e.target.value)} />
      </Field>
    </Dialog>
  )
}

function ScheduleDialog({ item, onClose }: { item: ContentItem; onClose: () => void }) {
  const { t } = useTranslation()
  const toast = useToast()
  const { businessId } = useWorkspace()
  const accounts = useSocialAccounts(businessId)
  const schedule = useScheduleContent(businessId, item.id ?? '')
  const platformName = usePlatformName()
  const options = (accounts.data ?? []).filter((a) => a.platformCode === item.platformCode && a.status?.code === 'Connected')
  const [accountId, setAccountId] = useState('')
  const [when, setWhen] = useState('')
  const chosen = accountId || options[0]?.id || ''

  return (
    <Dialog
      open
      onClose={onClose}
      title={t('content.schedule')}
      description={t('content.scheduleHint')}
      footer={
        <>
          <Button variant="ghost" onClick={onClose}>{t('common.cancel')}</Button>
          <Button
            variant="sun"
            disabled={schedule.isPending || options.length === 0}
            onClick={() =>
              schedule.mutate(
                { socialAccountId: chosen || null, publishAt: fromDateTimeInput(when) },
                { onSuccess: () => { toast.success(t('content.scheduled')); onClose() }, onError: (e) => toast.error(e) },
              )
            }
          >
            {schedule.isPending ? <Spinner /> : <CalendarClock aria-hidden />}
            {t('content.confirmSchedule')}
          </Button>
        </>
      }
    >
      {accounts.isPending ? (
        <SectionSpinner />
      ) : options.length === 0 ? (
        <Alert tone="info" title={t('content.noAccount', { platform: platformName(item.platformCode) })}>
          <Link to="/settings/accounts" className="font-semibold underline">{t('content.connectAccount')}</Link>
        </Alert>
      ) : (
        <div className="space-y-4">
          <Field label={t('content.account')}>
            <Select value={chosen} onChange={(e) => setAccountId(e.target.value)}>
              {options.map((a) => <option key={a.id} value={a.id}>{a.handle ? `@${a.handle}` : a.externalAccountId}</option>)}
            </Select>
          </Field>
          <Field label={t('content.publishAt')} optional hint={t('content.publishAtHint')}>
            <Input type="datetime-local" value={when} onChange={(e) => setWhen(e.target.value)} />
          </Field>
        </div>
      )}
    </Dialog>
  )
}

function useJobFor(contentItemId: string): PublicationJob | undefined {
  const { businessId } = useWorkspace()
  const jobs = usePublicationJobs(businessId)
  return [...(jobs.data?.items ?? [])]
    .filter((j) => j.contentItemId === contentItemId)
    .sort((a, b) => Date.parse(b.createdAt ?? '') - Date.parse(a.createdAt ?? ''))[0]
}

function ScheduledJob({ contentItemId }: { contentItemId: string }) {
  const { t, i18n } = useTranslation()
  const toast = useToast()
  const session = useSession()
  const { businessId, business } = useWorkspace()
  const job = useJobFor(contentItemId)
  const cancel = useCancelJob(businessId)
  if (!job) return null
  return (
    <div className="flex w-full flex-wrap items-center gap-3 rounded-2xl bg-surface-2 p-4">
      <CalendarClock className="size-5 text-brand-500" aria-hidden />
      <span className="flex-1 text-sm">
        {t('content.willPublish', { time: formatDateTime(job.scheduledAt, i18n.language, business.timeZoneId ?? undefined) })}
      </span>
      <StatusBadge status={job.status} />
      {can(session.tenant.role, 'edit') && ['Queued', 'Retry'].includes(job.status?.code ?? '') && (
        <Button variant="ghost" size="sm" disabled={cancel.isPending} onClick={() => cancel.mutate(job.id ?? '', { onSuccess: () => toast.success(t('content.jobCanceled')), onError: (e) => toast.error(e) })}>
          {t('content.unschedule')}
        </Button>
      )}
    </div>
  )
}

function PublicationCard({ item }: { item: ContentItem }) {
  const { t, i18n } = useTranslation()
  const toast = useToast()
  const session = useSession()
  const { businessId } = useWorkspace()
  const job = useJobFor(item.id ?? '')
  const publication = job?.publication
  const metrics = usePublicationMetrics(businessId, publication?.id)
  const refresh = useRefreshMetrics(businessId, publication?.id ?? '')

  if (item.status?.code !== 'Published' && !job?.lastError) return null
  if (!publication)
    return job?.lastError ? (
      <Alert title={t('content.publishFailed')}>{job.lastError}</Alert>
    ) : null

  const latest = new Map<string, number>()
  for (const point of [...(metrics.data ?? [])].sort((a, b) => Date.parse(a.observedAt ?? '') - Date.parse(b.observedAt ?? '')))
    latest.set(point.metricCode ?? '', point.value ?? 0)

  return (
    <Card className="p-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="text-lg font-bold">{t('content.published')}</h2>
          <p className="text-sm text-muted">{formatDateTime(publication.publishedAt, i18n.language)}</p>
        </div>
        <div className="flex gap-2">
          {publication.externalUrl && (
            <a href={publication.externalUrl} target="_blank" rel="noreferrer" className="inline-flex h-9 items-center gap-2 rounded-xl border border-line px-3 text-sm font-semibold hover:bg-surface-2">
              <ExternalLink className="size-4" aria-hidden />
              {t('content.viewPost')}
            </a>
          )}
          {can(session.tenant.role, 'edit') && (
            <Button variant="secondary" size="sm" disabled={refresh.isPending} onClick={() => refresh.mutate(undefined, { onError: (e) => toast.error(e) })}>
              {refresh.isPending ? <Spinner /> : <RefreshCw aria-hidden />}
              {t('content.refreshMetrics')}
            </Button>
          )}
        </div>
      </div>
      {latest.size === 0 ? (
        <p className="mt-4 text-sm text-muted">{t('content.noMetrics')}</p>
      ) : (
        <dl className="mt-5 grid grid-cols-2 gap-3 sm:grid-cols-4">
          {[...latest.entries()].map(([code, value]) => (
            <div key={code} className="rounded-2xl bg-surface-2 p-3">
              <dt className="text-xs text-muted">{enumLabel(t, 'metric', code)}</dt>
              <dd className="text-xl font-bold">{formatNumber(value, i18n.language)}</dd>
            </div>
          ))}
        </dl>
      )}
    </Card>
  )
}
