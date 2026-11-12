import { CornerDownLeft, EyeOff, Send } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { can } from '@/auth/roles'
import { useSession } from '@/auth/use-session'
import { StatusBadge } from '@/components/status-badge'
import { Badge, type BadgeTone } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Spinner } from '@/components/ui/spinner'
import { Textarea } from '@/components/ui/textarea'
import { useToast } from '@/components/ui/toast-context'
import { usePlatformName } from '@/features/platforms/api'
import { useWorkspace } from '@/features/workspace/workspace-context'
import { initials } from '@/lib/initials'
import { formatRelative } from '@/lib/format'
import { useIgnoreComment, useReplyToComment, type Comment } from './api'

const categoryTones: Record<string, BadgeTone> = {
  Complaint: 'coral', Question: 'grape', PurchaseIntent: 'green', Praise: 'sun', Spam: 'gray', Other: 'gray',
}

export function CommentCard({ comment }: { comment: Comment }) {
  const { t, i18n } = useTranslation()
  const toast = useToast()
  const session = useSession()
  const { businessId } = useWorkspace()
  const platformName = usePlatformName()
  const reply = useReplyToComment(businessId)
  const ignore = useIgnoreComment(businessId)
  const suggestion = comment.suggestedReply ?? ''
  const [text, setText] = useState(suggestion)
  const [usedSuggestion, setUsedSuggestion] = useState(suggestion)

  // The suggestion arrives after "analyze"; it fills the box unless the user already wrote something else.
  if (suggestion !== usedSuggestion) {
    setUsedSuggestion(suggestion)
    if (text.trim() === '' || text === usedSuggestion) setText(suggestion)
  }
  const status = comment.status?.code ?? ''
  const open = status === 'New' || status === 'Analyzed'
  const canReview = can(session.tenant.role, 'review')
  const canEdit = can(session.tenant.role, 'edit')
  const name = comment.authorName || comment.authorUsername || t('comments.customer')

  return (
    <li className="rounded-2xl border border-line bg-surface p-5 shadow-sm">
      <div className="flex items-start gap-3">
        <span className="grid size-10 shrink-0 place-items-center rounded-full bg-linear-to-br from-grape-400 to-brand-500 text-sm font-bold text-white">
          {initials(name)}
        </span>
        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-2">
            <span className="font-semibold">{name}</span>
            {comment.authorUsername && comment.authorName && <span className="text-sm text-muted" dir="ltr">@{comment.authorUsername}</span>}
            <span className="text-xs text-muted">· {platformName(comment.platform)} · {formatRelative(comment.postedAt, i18n.language)}</span>
          </div>
          <p className="mt-1.5 leading-relaxed [overflow-wrap:anywhere]" dir="auto">{comment.body}</p>
          <div className="mt-2 flex flex-wrap items-center gap-2">
            {comment.category && <Badge tone={categoryTones[comment.category]}>{t(`comments.categories.${comment.category}`)}</Badge>}
            {comment.needsReply && open && <Badge tone="sun">{t('comments.needsReply')}</Badge>}
            <StatusBadge status={comment.status} />
            {comment.contentItemId && (
              <Link to={`/content/${comment.contentItemId}`} className="text-xs font-semibold text-brand-600 hover:underline dark:text-brand-400">
                {t('comments.onPost')}
              </Link>
            )}
          </div>
        </div>
      </div>

      {status === 'Replied' && comment.replyBody && (
        <div className="ms-13 mt-3 flex gap-2 rounded-xl bg-brand-50 p-3 text-sm dark:bg-brand-900/40">
          <CornerDownLeft className="mt-0.5 size-4 shrink-0 text-brand-500" aria-hidden />
          <p dir="auto" className="[overflow-wrap:anywhere]">{comment.replyBody}</p>
        </div>
      )}

      {open && (canReview || canEdit) && (
        <div className="mt-4 space-y-2 sm:ms-13">
          {canReview && (
            <>
              <label htmlFor={`reply-${comment.id}`} className="text-sm font-semibold">
                {comment.suggestedReply ? t('comments.suggested') : t('comments.yourReply')}
              </label>
              <Textarea id={`reply-${comment.id}`} rows={2} dir="auto" maxLength={2000} value={text} onChange={(e) => setText(e.target.value)} placeholder={t('comments.replyPlaceholder')} />
            </>
          )}
          <div className="flex flex-wrap gap-2">
            {canReview && (
              <Button
                size="sm"
                disabled={reply.isPending || text.trim() === ''}
                onClick={() => reply.mutate({ id: comment.id ?? '', text: text.trim() }, { onSuccess: () => toast.success(t('comments.replied')), onError: (e) => toast.error(e) })}
              >
                {reply.isPending ? <Spinner /> : <Send aria-hidden />}
                {t('comments.sendReply')}
              </Button>
            )}
            {canEdit && (
              <Button
                size="sm"
                variant="ghost"
                disabled={ignore.isPending}
                onClick={() => ignore.mutate(comment.id ?? '', { onSuccess: () => toast.success(t('comments.ignored')), onError: (e) => toast.error(e) })}
              >
                <EyeOff aria-hidden />
                {t('comments.ignore')}
              </Button>
            )}
          </div>
        </div>
      )}
    </li>
  )
}
