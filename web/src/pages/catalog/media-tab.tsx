import { CircleAlert, FileText, Film, Images, Sparkles, Upload } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router'
import { can } from '@/auth/roles'
import { useSession } from '@/auth/use-session'
import { EmptyState } from '@/components/empty-state'
import { ErrorState } from '@/components/error-state'
import { FilterChips } from '@/components/filter-chips'
import { Pagination } from '@/components/pagination'
import { Button } from '@/components/ui/button'
import { SectionSpinner } from '@/components/ui/spinner'
import { mediaKinds, useMedia, type MediaAsset, type MediaKind } from '@/features/media/api'
import { MediaDetailsDialog } from '@/features/media/media-details-dialog'
import { MediaImage } from '@/features/media/media-image'
import { UploadDialog } from '@/features/media/upload-dialog'
import { useWorkspace } from '@/features/workspace/workspace-context'
import { cn } from '@/lib/cn'

export function MediaTab() {
  const { t } = useTranslation()
  const session = useSession()
  const { businessId } = useWorkspace()
  const [params, setParams] = useSearchParams()
  const kind = (params.get('kind') ?? '') as MediaKind | ''
  const page = Number(params.get('page') ?? 1)
  const media = useMedia(businessId, { kind, page, pageSize: 24 })
  const [uploading, setUploading] = useState(false)
  const [selected, setSelected] = useState<MediaAsset | null>(null)

  const setParam = (key: string, value: string) => {
    const next = new URLSearchParams(params)
    if (value) next.set(key, value)
    else next.delete(key)
    if (key !== 'page') next.delete('page')
    setParams(next)
  }

  const items = media.data?.items ?? []

  return (
    <>
      <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
        <FilterChips
          label={t('media.kind')}
          value={kind}
          onChange={(v) => setParam('kind', v)}
          options={[{ value: '' as const, label: t('common.all') }, ...mediaKinds.map((k) => ({ value: k, label: t(`media.kinds.${k}`) }))]}
        />
        {can(session.tenant.role, 'edit') && (
          <Button onClick={() => setUploading(true)}>
            <Upload aria-hidden />
            {t('media.uploadFiles')}
          </Button>
        )}
      </div>
      {media.isPending ? (
        <SectionSpinner />
      ) : media.isError ? (
        <ErrorState error={media.error} onRetry={() => void media.refetch()} />
      ) : items.length === 0 ? (
        <EmptyState icon={Images} title={kind ? t('media.emptyKind') : t('media.emptyLibrary')} hint={t('media.emptyHint')} />
      ) : (
        <>
          <ul className={cn('grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6', media.isFetching && 'opacity-70')}>
            {items.map((asset) => {
              const isImage = asset.mimeType?.startsWith('image/')
              const Icon = asset.mimeType?.startsWith('video/') ? Film : FileText
              const issues = asset.qualityIssues?.length ?? 0
              return (
                <li key={asset.id}>
                  <button
                    type="button"
                    onClick={() => setSelected(asset)}
                    className="group block w-full overflow-hidden rounded-2xl border border-line bg-surface text-start shadow-sm transition hover:-translate-y-0.5 hover:shadow-md"
                    aria-label={`${t(`media.kinds.${asset.kind}`)}${asset.visualDescription ? ` — ${asset.visualDescription}` : ''}`}
                  >
                    <div className="relative">
                      {isImage ? (
                        <MediaImage contentUrl={asset.contentUrl} mimeType={asset.mimeType} alt="" className="aspect-square w-full" />
                      ) : (
                        <div className="grid aspect-square w-full place-items-center bg-surface-2 text-muted">
                          <Icon className="size-10" aria-hidden />
                        </div>
                      )}
                      {issues > 0 && (
                        <span className="absolute top-2 start-2 inline-flex items-center gap-1 rounded-full bg-coral-500 px-2 py-0.5 text-xs font-bold text-white">
                          <CircleAlert className="size-3" aria-hidden />
                          {issues}
                        </span>
                      )}
                      {isImage && asset.describedAt && issues === 0 && (
                        <span className="absolute top-2 start-2 grid size-6 place-items-center rounded-full bg-brand-500 text-white" title={t('media.goodQuality')}>
                          <Sparkles className="size-3.5" aria-hidden />
                        </span>
                      )}
                    </div>
                    <span className="block truncate px-3 py-2 text-xs font-semibold text-muted">{t(`media.kinds.${asset.kind}`)}</span>
                  </button>
                </li>
              )
            })}
          </ul>
          <Pagination page={page} totalPages={media.data.totalPages ?? 1} onChange={(p) => setParam('page', String(p))} />
        </>
      )}
      <UploadDialog open={uploading} onClose={() => setUploading(false)} />
      <MediaDetailsDialog asset={selected} onClose={() => setSelected(null)} />
    </>
  )
}
