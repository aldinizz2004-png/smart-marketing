import { ArrowRight, Bot, Check, CircleAlert, Lock, LockOpen, Send, Sparkles, UserRound } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { can } from '@/auth/roles'
import { useSession } from '@/auth/use-session'
import { ErrorState } from '@/components/error-state'
import { StatusBadge } from '@/components/status-badge'
import { Alert } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { SectionSpinner, Spinner } from '@/components/ui/spinner'
import { Textarea } from '@/components/ui/textarea'
import { useToast } from '@/components/ui/toast-context'
import { useWorkspace } from '@/features/workspace/workspace-context'
import { cn } from '@/lib/cn'
import { formatDateTime } from '@/lib/format'
import { useChangeConversationStatus, useConversation, useSendMessage, useSuggestReply, type Message } from './api'

export function ConversationThread({ id, backTo }: { id: string; backTo: string }) {
  const { t, i18n } = useTranslation()
  const { businessId, business } = useWorkspace()
  const session = useSession()
  const toast = useToast()
  const query = useConversation(businessId, id)
  const status = useChangeConversationStatus(businessId, id)
  const suggest = useSuggestReply(businessId, id)
  const bottom = useRef<HTMLDivElement>(null)
  const count = query.data?.messages?.length ?? 0

  useEffect(() => {
    bottom.current?.scrollIntoView?.({ block: 'end' })
  }, [count, id])

  if (query.isPending) return <SectionSpinner />
  if (query.isError) return <ErrorState error={query.error} onRetry={() => void query.refetch()} />

  const conversation = query.data.conversation ?? {}
  const closed = conversation.status?.code === 'Closed'
  const name = conversation.customerName || conversation.customerPhone || t('inbox.customer')
  const canEdit = can(session.tenant.role, 'edit')
  const canReview = can(session.tenant.role, 'review')
  const timeZone = business.timeZoneId ?? undefined

  return (
    <section aria-label={name} className="flex min-h-[32rem] flex-col overflow-hidden rounded-2xl border border-line bg-surface lg:h-[calc(100dvh-12rem)]">
      <header className="flex flex-wrap items-center gap-3 border-b border-line p-4">
        <Link to={backTo} className="grid size-9 place-items-center rounded-full hover:bg-surface-2 lg:hidden" aria-label={t('inbox.back')}>
          <ArrowRight className="size-5 ltr:rotate-180" aria-hidden />
        </Link>
        <div className="min-w-0 flex-1">
          <h2 className="truncate font-bold">{name}</h2>
          {conversation.customerPhone && <p className="truncate text-sm text-muted"><span dir="ltr">+{conversation.customerPhone.replace(/^\+/, '')}</span></p>}
        </div>
        <div className="flex w-full flex-wrap items-center gap-2 sm:w-auto">
          {conversation.lastIntent && <Badge tone="grape">{t(`inbox.intents.${conversation.lastIntent}`)}</Badge>}
          <StatusBadge status={conversation.status} />
          {canEdit && (
            <Button
              size="sm"
              variant={closed ? 'soft' : 'ghost'}
              disabled={status.isPending}
              onClick={() =>
                status.mutate(closed ? 'Open' : 'Closed', {
                  onSuccess: () => toast.success(closed ? t('inbox.reopened') : t('inbox.closed')),
                  onError: (e) => toast.error(e),
                })
              }
            >
              {status.isPending ? <Spinner /> : closed ? <LockOpen aria-hidden /> : <Lock aria-hidden />}
              {closed ? t('inbox.reopen') : t('inbox.close')}
            </Button>
          )}
        </div>
      </header>

      {conversation.handoffReason && !closed && (
        <div className="border-b border-line p-3">
          <Alert tone="info" title={t('inbox.handoff')}>{conversation.handoffReason}</Alert>
        </div>
      )}

      <div className="flex-1 space-y-3 overflow-y-auto bg-surface-2/50 p-4" aria-live="polite">
        {(query.data.messages ?? []).map((m) => <MessageBubble key={m.id} message={m} language={i18n.language} timeZone={timeZone} />)}
        <div ref={bottom} />
      </div>

      {canReview && !closed ? (
        <Composer
          key={id}
          id={id}
          suggestion={query.data.suggestedReply ?? ''}
          canReplyFreely={conversation.canReplyFreely ?? false}
          windowEndsAt={conversation.replyWindowEndsAt}
          timeZone={timeZone}
          canSuggest={canEdit}
          suggesting={suggest.isPending}
          onSuggest={() => suggest.mutate(undefined, { onError: (e) => toast.error(e) })}
        />
      ) : closed ? (
        <p className="border-t border-line p-4 text-center text-sm text-muted">{t('inbox.closedHint')}</p>
      ) : null}
    </section>
  )
}

function MessageBubble({ message, language, timeZone }: { message: Message; language: string; timeZone?: string }) {
  const { t } = useTranslation()
  const inbound = message.direction === 'Inbound'
  const failed = message.deliveryStatus === 'failed' || !!message.error
  return (
    <div className={cn('flex', inbound ? 'justify-start' : 'justify-end')}>
      <div
        className={cn(
          'max-w-[85%] rounded-2xl px-4 py-2.5 shadow-sm sm:max-w-[70%]',
          inbound ? 'rounded-ss-sm bg-surface' : 'rounded-se-sm bg-linear-to-br from-brand-600 to-brand-500 text-white',
          failed && 'from-coral-500 to-coral-500',
        )}
      >
        {!inbound && (
          <p className="mb-1 flex items-center gap-1 text-xs font-semibold opacity-80">
            {message.author === 'Ai' ? <Bot className="size-3.5" aria-hidden /> : <UserRound className="size-3.5" aria-hidden />}
            {message.author === 'Ai' ? t('inbox.byAi') : t('inbox.byTeam')}
          </p>
        )}
        <p className="whitespace-pre-line leading-relaxed [overflow-wrap:anywhere]" dir="auto">{message.body}</p>
        <p className={cn('mt-1 flex items-center justify-end gap-1 text-[11px]', inbound ? 'text-muted' : 'opacity-75')}>
          {formatDateTime(message.sentAt, language, timeZone)}
          {!inbound && !failed && <Check className="size-3" aria-label={message.deliveryStatus ?? ''} />}
          {failed && <CircleAlert className="size-3" aria-hidden />}
        </p>
        {failed && message.error && <p className="mt-1 text-xs">{message.error}</p>}
      </div>
    </div>
  )
}

type ComposerProps = {
  id: string
  suggestion: string
  canReplyFreely: boolean
  windowEndsAt?: string | null
  timeZone?: string
  canSuggest: boolean
  suggesting: boolean
  onSuggest: () => void
}

function Composer({ id, suggestion, canReplyFreely, windowEndsAt, timeZone, canSuggest, suggesting, onSuggest }: ComposerProps) {
  const { t, i18n } = useTranslation()
  const toast = useToast()
  const { businessId } = useWorkspace()
  const send = useSendMessage(businessId, id)
  const [text, setText] = useState(suggestion)
  const [usedSuggestion, setUsedSuggestion] = useState(suggestion)
  const [template, setTemplate] = useState('')
  const [languageCode, setLanguageCode] = useState('ar')

  // A new AI suggestion replaces the draft only while the draft is empty or still the previous suggestion.
  if (suggestion !== usedSuggestion) {
    setUsedSuggestion(suggestion)
    if (text.trim() === '' || text === usedSuggestion) setText(suggestion)
  }

  if (!canReplyFreely) {
    return (
      <form
        className="space-y-3 border-t border-line p-4"
        onSubmit={(e) => {
          e.preventDefault()
          send.mutate(
            { templateName: template.trim(), languageCode: languageCode.trim() || 'ar' },
            { onSuccess: () => { setTemplate(''); toast.success(t('inbox.sent')) }, onError: (e) => toast.error(e) },
          )
        }}
      >
        <Alert tone="info" title={t('inbox.windowClosed')}>{t('inbox.windowClosedHint')}</Alert>
        <div className="flex flex-wrap items-end gap-2">
          <label className="min-w-40 flex-1 space-y-1">
            <span className="text-sm font-semibold">{t('inbox.templateName')}</span>
            <Input dir="ltr" value={template} onChange={(e) => setTemplate(e.target.value)} placeholder="order_followup" />
          </label>
          <label className="w-24 space-y-1">
            <span className="text-sm font-semibold">{t('inbox.templateLanguage')}</span>
            <Input dir="ltr" value={languageCode} maxLength={10} onChange={(e) => setLanguageCode(e.target.value)} />
          </label>
          <Button type="submit" disabled={send.isPending || template.trim() === ''}>
            {send.isPending ? <Spinner /> : <Send aria-hidden />}
            {t('inbox.sendTemplate')}
          </Button>
        </div>
      </form>
    )
  }

  return (
    <form
      className="space-y-2 border-t border-line p-4"
      onSubmit={(e) => {
        e.preventDefault()
        send.mutate({ text: text.trim() }, { onSuccess: () => { setText(''); toast.success(t('inbox.sent')) }, onError: (e) => toast.error(e) })
      }}
    >
      <div className="flex items-center justify-between gap-2">
        <label htmlFor={`message-${id}`} className="text-sm font-semibold">
          {suggestion && text === suggestion ? t('inbox.suggested') : t('inbox.yourMessage')}
        </label>
        {windowEndsAt && <span className="text-xs text-muted">{t('inbox.windowEnds', { time: formatDateTime(windowEndsAt, i18n.language, timeZone) })}</span>}
      </div>
      <Textarea
        id={`message-${id}`}
        rows={3}
        dir="auto"
        maxLength={4096}
        value={text}
        onChange={(e) => setText(e.target.value)}
        placeholder={t('inbox.messagePlaceholder')}
        onKeyDown={(e) => {
          if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) e.currentTarget.form?.requestSubmit()
        }}
      />
      <div className="flex flex-wrap gap-2">
        <Button type="submit" disabled={send.isPending || text.trim() === ''}>
          {send.isPending ? <Spinner /> : <Send aria-hidden />}
          {t('common.send')}
        </Button>
        {canSuggest && (
          <Button type="button" variant="sun" disabled={suggesting} onClick={onSuggest}>
            {suggesting ? <Spinner /> : <Sparkles aria-hidden />}
            {suggestion ? t('inbox.suggestAgain') : t('inbox.suggest')}
          </Button>
        )}
      </div>
    </form>
  )
}
