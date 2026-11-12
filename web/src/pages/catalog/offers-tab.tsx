import { CalendarRange, Pause, Pencil, Play, Plus, Square, Tag, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router'
import { can } from '@/auth/roles'
import { useSession } from '@/auth/use-session'
import { ConfirmDialog } from '@/components/confirm-dialog'
import { EmptyState } from '@/components/empty-state'
import { ErrorState } from '@/components/error-state'
import { FilterChips } from '@/components/filter-chips'
import { Pagination } from '@/components/pagination'
import { StatusBadge } from '@/components/status-badge'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { SectionSpinner } from '@/components/ui/spinner'
import { useToast } from '@/components/ui/toast-context'
import { useChangeOfferStatus, useDeleteOffer, useOffers, type Offer } from '@/features/catalog/api'
import { OfferFormDialog } from '@/features/catalog/offer-form-dialog'
import { canDeleteOffer, canEditOffer, offerNext, offerStatuses, type OfferStatus } from '@/features/catalog/offer-rules'
import { useWorkspace } from '@/features/workspace/workspace-context'
import { cn } from '@/lib/cn'
import { formatDateTime, formatMoney, formatNumber } from '@/lib/format'

const moveIcons = { Active: Play, Paused: Pause, Ended: Square, Draft: Pencil } as const

export function OffersTab() {
  const { t, i18n } = useTranslation()
  const toast = useToast()
  const session = useSession()
  const { businessId, business } = useWorkspace()
  const [params, setParams] = useSearchParams()
  const status = params.get('status') ?? ''
  const page = Number(params.get('page') ?? 1)
  const offers = useOffers(businessId, { status, page, pageSize: 20 })
  const move = useChangeOfferStatus(businessId)
  const remove = useDeleteOffer(businessId)
  const [editing, setEditing] = useState<Offer | 'new' | null>(null)
  const [deleting, setDeleting] = useState<Offer | null>(null)
  const [ending, setEnding] = useState<Offer | null>(null)
  const canEdit = can(session.tenant.role, 'edit')
  const timeZone = business.timeZoneId ?? undefined

  const setParam = (key: string, value: string) => {
    const next = new URLSearchParams(params)
    if (value) next.set(key, value)
    else next.delete(key)
    if (key !== 'page') next.delete('page')
    setParams(next)
  }

  const changeStatus = (offer: Offer, to: OfferStatus) =>
    move.mutate(
      { id: offer.id ?? '', status: to },
      { onSuccess: () => { toast.success(t(`catalog.moved.${to}`)); setEnding(null) }, onError: (e) => { toast.error(e); setEnding(null) } },
    )

  const items = offers.data?.items ?? []

  return (
    <>
      <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
        <FilterChips
          label={t('content.filterStatus')}
          value={status}
          onChange={(v) => setParam('status', v)}
          options={[{ value: '', label: t('common.all') }, ...offerStatuses.map((s) => ({ value: s, label: t(`catalog.offerStatuses.${s}`) }))]}
        />
        {canEdit && (
          <Button onClick={() => setEditing('new')}>
            <Plus aria-hidden />
            {t('catalog.newOffer')}
          </Button>
        )}
      </div>
      {offers.isPending ? (
        <SectionSpinner />
      ) : offers.isError ? (
        <ErrorState error={offers.error} onRetry={() => void offers.refetch()} />
      ) : items.length === 0 ? (
        <EmptyState icon={Tag} title={status ? t('catalog.noOffersFiltered') : t('catalog.noOffers')} hint={status ? undefined : t('catalog.noOffersHint')} />
      ) : (
        <>
          <ul className={cn('grid gap-3 lg:grid-cols-2', offers.isFetching && 'opacity-70')}>
            {items.map((offer) => {
              const code = (offer.status?.code ?? 'Draft') as OfferStatus
              return (
                <li key={offer.id} className={cn('flex flex-col rounded-2xl border border-line bg-surface p-5 shadow-sm', code === 'Ended' && 'opacity-70')}>
                  <div className="flex items-start justify-between gap-3">
                    <div className="min-w-0">
                      <h3 className="font-bold [overflow-wrap:anywhere]">{offer.title}</h3>
                      {offer.description && <p className="mt-1 line-clamp-2 text-sm text-muted">{offer.description}</p>}
                    </div>
                    <span className="shrink-0 rounded-xl bg-linear-to-br from-sun-300 to-sun-400 px-3 py-1.5 text-lg font-extrabold text-amber-950">
                      {offer.discountPercent != null
                        ? t('catalog.percentOff', { value: formatNumber(offer.discountPercent, i18n.language, 0) })
                        : formatMoney(offer.offerPrice ?? 0, offer.currency ?? 'ILS', i18n.language)}
                    </span>
                  </div>
                  <p className="mt-3 flex items-center gap-1.5 text-sm text-muted">
                    <CalendarRange className="size-4 shrink-0" aria-hidden />
                    {t('catalog.dateRange', { from: formatDateTime(offer.startsAt, i18n.language, timeZone), to: formatDateTime(offer.endsAt, i18n.language, timeZone) })}
                  </p>
                  <div className="mt-auto flex flex-wrap items-center gap-2 pt-4">
                    <StatusBadge status={offer.status} />
                    {offer.isRunningNow && <Badge tone="green">{t('catalog.runningNow')}</Badge>}
                    {canEdit && (
                      <div className="ms-auto flex flex-wrap gap-1">
                        {offerNext[code].map((to) => {
                          const Icon = moveIcons[to]
                          return (
                            <Button
                              key={to}
                              size="sm"
                              variant={to === 'Active' ? 'soft' : 'ghost'}
                              disabled={move.isPending}
                              onClick={() => (to === 'Ended' ? setEnding(offer) : changeStatus(offer, to))}
                            >
                              <Icon aria-hidden />
                              {t(`catalog.moves.${to}`)}
                            </Button>
                          )
                        })}
                        {canEditOffer(code) && (
                          <Button size="sm" variant="ghost" onClick={() => setEditing(offer)} aria-label={`${t('common.edit')} ${offer.title}`}>
                            <Pencil aria-hidden />
                          </Button>
                        )}
                        {canDeleteOffer(code) && (
                          <Button size="sm" variant="ghost" onClick={() => setDeleting(offer)} aria-label={`${t('common.remove')} ${offer.title}`} className="text-coral-500">
                            <Trash2 aria-hidden />
                          </Button>
                        )}
                      </div>
                    )}
                  </div>
                </li>
              )
            })}
          </ul>
          <Pagination page={page} totalPages={offers.data.totalPages ?? 1} onChange={(p) => setParam('page', String(p))} />
        </>
      )}
      <OfferFormDialog open={editing !== null} onClose={() => setEditing(null)} offer={editing === 'new' || editing === null ? undefined : editing} />
      <ConfirmDialog
        open={ending !== null}
        title={t('catalog.endOffer', { name: ending?.title })}
        description={t('catalog.endOfferHint')}
        confirmLabel={t('catalog.moves.Ended')}
        danger
        busy={move.isPending}
        onClose={() => setEnding(null)}
        onConfirm={() => ending && changeStatus(ending, 'Ended')}
      />
      <ConfirmDialog
        open={deleting !== null}
        title={t('catalog.deleteOffer', { name: deleting?.title })}
        confirmLabel={t('common.remove')}
        danger
        busy={remove.isPending}
        onClose={() => setDeleting(null)}
        onConfirm={() =>
          remove.mutate(deleting?.id ?? '', {
            onSuccess: () => { toast.success(t('catalog.deleted')); setDeleting(null) },
            onError: (e) => { toast.error(e); setDeleting(null) },
          })
        }
      />
    </>
  )
}
