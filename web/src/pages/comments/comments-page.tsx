import { MessageCircleHeart, RefreshCw, Sparkles } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router'
import { can } from '@/auth/roles'
import { useSession } from '@/auth/use-session'
import { EmptyState } from '@/components/empty-state'
import { ErrorState } from '@/components/error-state'
import { FilterChips } from '@/components/filter-chips'
import { PageHeader } from '@/components/page-header'
import { Pagination } from '@/components/pagination'
import { Button } from '@/components/ui/button'
import { Select } from '@/components/ui/select'
import { SectionSpinner, Spinner } from '@/components/ui/spinner'
import { useToast } from '@/components/ui/toast-context'
import { commentCategories, commentStatuses, useAnalyzeComments, useComments, useNewCommentsCount, useSyncComments } from '@/features/comments/api'
import { CommentCard } from '@/features/comments/comment-card'
import { usePlatforms } from '@/features/platforms/api'
import { useSocialAccounts } from '@/features/social/api'
import { useWorkspace } from '@/features/workspace/workspace-context'
import { cn } from '@/lib/cn'

export function CommentsPage() {
  const { t } = useTranslation()
  const toast = useToast()
  const session = useSession()
  const { businessId } = useWorkspace()
  const [params, setParams] = useSearchParams()
  const status = params.get('status') ?? ''
  const category = params.get('category') ?? ''
  const needsReply = params.get('needsReply') === 'true'
  const platform = params.get('platform') ?? ''
  const page = Number(params.get('page') ?? 1)
  const comments = useComments(businessId, { status, category, needsReply, platform, page })
  const newCount = useNewCommentsCount(businessId)
  const sync = useSyncComments(businessId)
  const analyze = useAnalyzeComments(businessId)
  const canEdit = can(session.tenant.role, 'edit')
  const platforms = usePlatforms()
  const accounts = useSocialAccounts(businessId)
  const connected = new Set((accounts.data ?? []).map((a) => a.platformCode))
  const platformOptions = (platforms.data ?? []).filter((p) => connected.has(p.code) && p.code !== 'whatsapp')

  const update = (key: string, value: string) => {
    const next = new URLSearchParams(params)
    if (value) next.set(key, value)
    else next.delete(key)
    if (key !== 'page') next.delete('page')
    setParams(next)
  }

  return (
    <>
      <PageHeader
        title={t('comments.title')}
        subtitle={t('comments.subtitle')}
        actions={
          canEdit && (
            <div className="flex flex-wrap gap-2">
              <Button
                variant="secondary"
                disabled={sync.isPending}
                onClick={() =>
                  sync.mutate(undefined, {
                    onSuccess: (r) => toast.success(r.added ? t('comments.synced', { count: r.added }) : t('comments.syncedNone')),
                    onError: (e) => toast.error(e),
                  })
                }
              >
                {sync.isPending ? <Spinner /> : <RefreshCw aria-hidden />}
                {t('comments.sync')}
              </Button>
              <Button
                variant="sun"
                disabled={analyze.isPending || (newCount.data ?? 0) === 0}
                onClick={() =>
                  analyze.mutate(undefined, {
                    onSuccess: (r) => toast.success(t('comments.analyzed', { count: r.analyzed ?? 0, reply: r.needReply ?? 0 })),
                    onError: (e) => toast.error(e),
                  })
                }
              >
                {analyze.isPending ? <Spinner /> : <Sparkles aria-hidden />}
                {t('comments.analyze', { count: newCount.data ?? 0 })}
              </Button>
            </div>
          )
        }
      />
      <div className="mb-5 space-y-3">
        <FilterChips
          label={t('content.filterStatus')}
          value={status}
          onChange={(v) => update('status', v)}
          options={[{ value: '', label: t('common.all') }, ...commentStatuses.map((s) => ({ value: s, label: t(`comments.statuses.${s}`) }))]}
        />
        <div className="flex flex-wrap items-center gap-3">
          <FilterChips
            label={t('comments.filterCategory')}
            value={category}
            onChange={(v) => update('category', v)}
            options={[{ value: '', label: t('comments.allCategories') }, ...commentCategories.map((c) => ({ value: c, label: t(`comments.categories.${c}`) }))]}
          />
          <button
            type="button"
            aria-pressed={needsReply}
            onClick={() => update('needsReply', needsReply ? '' : 'true')}
            className={cn('rounded-full border px-3.5 py-1.5 text-sm font-semibold', needsReply ? 'border-sun-400 bg-sun-300/30 text-amber-800 dark:text-sun-300' : 'border-line text-muted')}
          >
            {t('comments.onlyNeedsReply')}
          </button>
          {platformOptions.length > 1 && (
            <div className="w-44">
              <Select aria-label={t('comments.filterPlatform')} value={platform} onChange={(e) => update('platform', e.target.value)} className="h-9 text-sm">
                <option value="">{t('comments.allPlatforms')}</option>
                {platformOptions.map((p) => <option key={p.code} value={p.code ?? ''}>{p.displayName}</option>)}
              </Select>
            </div>
          )}
        </div>
      </div>
      {comments.isPending ? (
        <SectionSpinner />
      ) : comments.isError ? (
        <ErrorState error={comments.error} onRetry={() => void comments.refetch()} />
      ) : (comments.data.items ?? []).length === 0 ? (
        <EmptyState icon={MessageCircleHeart} title={status || category || needsReply || platform ? t('comments.emptyFiltered') : t('comments.empty')} hint={t('comments.emptyHint')} />
      ) : (
        <>
          <ul className={cn('space-y-3', comments.isFetching && 'opacity-70')}>
            {comments.data.items!.map((comment) => <CommentCard key={comment.id} comment={comment} />)}
          </ul>
          <Pagination page={page} totalPages={comments.data.totalPages ?? 1} onChange={(p) => update('page', String(p))} />
        </>
      )}
    </>
  )
}
