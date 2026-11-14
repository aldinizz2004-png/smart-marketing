import { Eye, Heart, MessageCircle } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { EmptyState } from '@/components/empty-state'
import { ErrorState } from '@/components/error-state'
import { Pagination } from '@/components/pagination'
import { Badge } from '@/components/ui/badge'
import { Dialog } from '@/components/ui/dialog'
import { SectionSpinner } from '@/components/ui/spinner'
import { useWorkspace } from '@/features/workspace/workspace-context'
import { formatDate, formatNumber, formatPercent } from '@/lib/format'
import { useSourcePosts, type TrackedSource } from './api'

export function SourcePostsDialog({ source, onClose }: { source: TrackedSource | null; onClose: () => void }) {
  const { t, i18n } = useTranslation()
  const { businessId } = useWorkspace()
  const [page, setPage] = useState(1)
  const posts = useSourcePosts(businessId, source?.id, page)
  const name = source?.displayName || source?.handle || source?.url || ''

  const close = () => {
    setPage(1)
    onClose()
  }

  return (
    <Dialog open={!!source} onClose={close} size="lg" title={t('competitors.postsOf', { name })} description={t('competitors.postsHint')}>
      {posts.isPending ? (
        <SectionSpinner />
      ) : posts.isError ? (
        <ErrorState error={posts.error} onRetry={() => void posts.refetch()} />
      ) : (posts.data.items ?? []).length === 0 ? (
        <EmptyState icon={MessageCircle} title={t('competitors.noPosts')} hint={t('competitors.noPostsHint')} />
      ) : (
        <>
          <ul className="space-y-3">
            {posts.data.items!.map((post) => (
              <li key={post.id} className="rounded-xl border border-line p-3.5">
                <div className="mb-2 flex flex-wrap items-center gap-2 text-xs text-muted">
                  {post.format && <Badge tone="grape">{t(`enums.format.${post.format}`)}</Badge>}
                  {post.hookType && <Badge tone="sun">{t(`enums.hook.${post.hookType}`)}</Badge>}
                  <span>{formatDate(post.postedAt, i18n.language)}</span>
                  {post.postUrl && (
                    <a href={post.postUrl} target="_blank" rel="noreferrer" className="ms-auto font-semibold text-brand-700 hover:underline dark:text-brand-300">
                      {t('competitors.openPost')}
                    </a>
                  )}
                </div>
                {post.caption && <p className="line-clamp-4 whitespace-pre-line [overflow-wrap:anywhere]">{post.caption}</p>}
                <dl className="mt-3 flex flex-wrap gap-x-5 gap-y-1 text-sm text-muted">
                  <div className="flex items-center gap-1.5">
                    <Heart className="size-4" aria-hidden />
                    <dt className="sr-only">{t('competitors.likes')}</dt>
                    <dd>{formatNumber(post.likes, i18n.language)}</dd>
                  </div>
                  <div className="flex items-center gap-1.5">
                    <MessageCircle className="size-4" aria-hidden />
                    <dt className="sr-only">{t('competitors.comments')}</dt>
                    <dd>{formatNumber(post.comments, i18n.language)}</dd>
                  </div>
                  {post.views != null && (
                    <div className="flex items-center gap-1.5">
                      <Eye className="size-4" aria-hidden />
                      <dt className="sr-only">{t('competitors.views')}</dt>
                      <dd>{formatNumber(post.views, i18n.language)}</dd>
                    </div>
                  )}
                  {post.engagementRate != null && (
                    <div className="flex items-center gap-1.5">
                      <dt>{t('competitors.engagement')}</dt>
                      <dd className="font-semibold text-ink">{formatPercent(post.engagementRate, i18n.language)}</dd>
                    </div>
                  )}
                </dl>
              </li>
            ))}
          </ul>
          <Pagination page={page} totalPages={posts.data.totalPages ?? 1} onChange={setPage} />
        </>
      )}
    </Dialog>
  )
}
