import { Sparkles } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { formatRelative } from '@/lib/format'
import { initials } from '@/lib/initials'
import { cn } from '@/lib/cn'
import type { ConversationSummary } from './api'

export function ConversationList({ items, selectedId, search }: { items: ConversationSummary[]; selectedId?: string; search: string }) {
  const { t, i18n } = useTranslation()
  return (
    <ul className="divide-y divide-line overflow-hidden rounded-2xl border border-line bg-surface">
      {items.map((c) => {
        const name = c.customerName || c.customerPhone || t('inbox.customer')
        const waiting = c.status?.code === 'WaitingForOwner'
        const closed = c.status?.code === 'Closed'
        return (
          <li key={c.id}>
            <Link
              to={`/inbox/${c.id}${search}`}
              aria-current={selectedId === c.id ? 'page' : undefined}
              className={cn('flex gap-3 p-4 transition-colors hover:bg-surface-2', selectedId === c.id && 'bg-brand-50 dark:bg-brand-900/40', closed && 'opacity-60')}
            >
              <span className="relative grid size-11 shrink-0 place-items-center rounded-full bg-linear-to-br from-brand-500 to-brand-700 text-sm font-bold text-white">
                {initials(name)}
                {waiting && <span className="absolute -top-0.5 -end-0.5 size-3 rounded-full border-2 border-surface bg-coral-500" aria-hidden />}
              </span>
              <span className="min-w-0 flex-1">
                <span className="flex items-baseline justify-between gap-2">
                  <span className="truncate font-semibold">{name}</span>
                  <span className="shrink-0 text-xs text-muted">{formatRelative(c.lastMessageAt, i18n.language)}</span>
                </span>
                <span className="mt-0.5 block truncate text-sm text-muted" dir="auto">{c.lastMessage || '—'}</span>
                <span className="mt-1.5 flex flex-wrap items-center gap-1.5 text-xs font-semibold">
                  {waiting && <span className="rounded-full bg-coral-400/15 px-2 py-0.5 text-coral-500">{c.status?.text || t('inbox.statuses.WaitingForOwner')}</span>}
                  {c.lastIntent && <span className="rounded-full bg-surface-2 px-2 py-0.5 text-muted">{t(`inbox.intents.${c.lastIntent}`)}</span>}
                  {c.hasSuggestion && (
                    <span className="inline-flex items-center gap-1 rounded-full bg-sun-300/30 px-2 py-0.5 text-amber-800 dark:text-sun-300">
                      <Sparkles className="size-3" aria-hidden />
                      {t('inbox.hasSuggestion')}
                    </span>
                  )}
                </span>
              </span>
            </Link>
          </li>
        )
      })}
    </ul>
  )
}
