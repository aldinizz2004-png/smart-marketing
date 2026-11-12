import { MessagesSquare } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link, useLocation, useParams, useSearchParams } from 'react-router'
import { EmptyState } from '@/components/empty-state'
import { ErrorState } from '@/components/error-state'
import { FilterChips } from '@/components/filter-chips'
import { PageHeader } from '@/components/page-header'
import { Pagination } from '@/components/pagination'
import { Select } from '@/components/ui/select'
import { SectionSpinner } from '@/components/ui/spinner'
import { conversationIntents, conversationStatuses, useConversations } from '@/features/inbox/api'
import { ConversationList } from '@/features/inbox/conversation-list'
import { ConversationThread } from '@/features/inbox/conversation-thread'
import { useSocialAccounts } from '@/features/social/api'
import { useWorkspace } from '@/features/workspace/workspace-context'
import { buttonVariants } from '@/components/ui/button-variants'
import { cn } from '@/lib/cn'

export function InboxPage() {
  const { t } = useTranslation()
  const { id = '' } = useParams()
  const { search } = useLocation()
  const { businessId } = useWorkspace()
  const [params, setParams] = useSearchParams()
  const status = params.get('status') ?? ''
  const intent = params.get('intent') ?? ''
  const page = Number(params.get('page') ?? 1)
  const conversations = useConversations(businessId, { status, intent, page })
  const accounts = useSocialAccounts(businessId)
  const hasWhatsApp = (accounts.data ?? []).some((a) => a.platformCode === 'whatsapp' && a.status?.code === 'Connected')

  const update = (key: string, value: string) => {
    const next = new URLSearchParams(params)
    if (value) next.set(key, value)
    else next.delete(key)
    if (key !== 'page') next.delete('page')
    setParams(next)
  }

  const items = conversations.data?.items ?? []

  return (
    <>
      <div className={cn(id && 'hidden lg:block')}>
        <PageHeader title={t('inbox.title')} subtitle={t('inbox.subtitle')} />
        <div className="mb-5 flex flex-wrap items-center gap-3">
          <FilterChips
            label={t('content.filterStatus')}
            value={status}
            onChange={(v) => update('status', v)}
            options={[{ value: '', label: t('common.all') }, ...conversationStatuses.map((s) => ({ value: s, label: t(`inbox.statuses.${s}`) }))]}
          />
          <div className="w-44">
            <Select aria-label={t('inbox.filterIntent')} value={intent} onChange={(e) => update('intent', e.target.value)} className="h-9 text-sm">
              <option value="">{t('inbox.allIntents')}</option>
              {conversationIntents.map((i) => <option key={i} value={i}>{t(`inbox.intents.${i}`)}</option>)}
            </Select>
          </div>
        </div>
      </div>
      {accounts.isSuccess && !hasWhatsApp && items.length === 0 && !conversations.isPending ? (
        <EmptyState
          icon={MessagesSquare}
          title={t('inbox.noWhatsApp')}
          hint={t('inbox.noWhatsAppHint')}
          action={<Link to="/settings/accounts" className={buttonVariants()}>{t('inbox.connectWhatsApp')}</Link>}
        />
      ) : (
        <div className="grid gap-5 lg:grid-cols-[22rem_1fr]">
          <div className={cn(id && 'hidden lg:block')}>
            {conversations.isPending ? (
              <SectionSpinner />
            ) : conversations.isError ? (
              <ErrorState error={conversations.error} onRetry={() => void conversations.refetch()} />
            ) : items.length === 0 ? (
              <EmptyState icon={MessagesSquare} title={status || intent ? t('inbox.emptyFiltered') : t('inbox.empty')} hint={t('inbox.emptyHint')} />
            ) : (
              <>
                <ConversationList items={items} selectedId={id} search={search} />
                <Pagination page={page} totalPages={conversations.data.totalPages ?? 1} onChange={(p) => update('page', String(p))} />
              </>
            )}
          </div>
          <div className={cn(!id && 'hidden lg:block')}>
            {id ? (
              <ConversationThread id={id} backTo={`/inbox${search}`} />
            ) : (
              <div className="grid h-full min-h-80 place-items-center rounded-2xl border border-dashed border-line p-8 text-center text-muted">
                <div>
                  <MessagesSquare className="mx-auto mb-3 size-10 text-brand-500" aria-hidden />
                  {t('inbox.pick')}
                </div>
              </div>
            )}
          </div>
        </div>
      )}
    </>
  )
}
