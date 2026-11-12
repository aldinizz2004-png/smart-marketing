import { Download, FileText, Sparkles, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { can } from '@/auth/roles'
import { useSession } from '@/auth/use-session'
import { Alert } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { buttonVariants } from '@/components/ui/button-variants'
import { Dialog } from '@/components/ui/dialog'
import { Spinner } from '@/components/ui/spinner'
import { useToast } from '@/components/ui/toast-context'
import { useProducts } from '@/features/catalog/api'
import { useWorkspace } from '@/features/workspace/workspace-context'
import { formatBytes, formatDate } from '@/lib/format'
import { useDeleteMedia, useDescribeMedia, useMediaBlob, type MediaAsset } from './api'
import { MediaImage } from './media-image'

export function MediaDetailsDialog({ asset, onClose }: { asset: MediaAsset | null; onClose: () => void }) {
  const { t, i18n } = useTranslation()
  const toast = useToast()
  const session = useSession()
  const { businessId } = useWorkspace()
  const products = useProducts(businessId)
  const describe = useDescribeMedia(businessId)
  const remove = useDeleteMedia(businessId)
  const [confirming, setConfirming] = useState(false)
  const [current, setCurrent] = useState(asset)
  if (asset?.id !== current?.id) setCurrent(asset)
  const shown = current ?? asset
  const isImage = shown?.mimeType?.startsWith('image/') ?? false
  const isVideo = shown?.mimeType?.startsWith('video/') ?? false
  const file = useMediaBlob(shown && !isImage ? shown.contentUrl : null)
  const canEdit = can(session.tenant.role, 'edit')
  const product = products.data?.items?.find((p) => p.id === shown?.productId)

  const close = () => {
    setConfirming(false)
    onClose()
  }

  if (!shown) return null
  const issues = shown.qualityIssues ?? []

  return (
    <Dialog
      open={asset !== null}
      onClose={close}
      size="lg"
      title={t(`media.kinds.${shown.kind ?? 'ProductPhoto'}`)}
      footer={
        canEdit ? (
          confirming ? (
            <>
              <span className="me-auto text-sm font-semibold text-coral-500">{t('media.confirmDelete')}</span>
              <Button variant="ghost" onClick={() => setConfirming(false)}>{t('common.cancel')}</Button>
              <Button
                variant="danger"
                disabled={remove.isPending}
                onClick={() =>
                  remove.mutate(shown.id ?? '', {
                    onSuccess: () => { toast.success(t('media.deleted')); close() },
                    onError: (e) => { toast.error(e); setConfirming(false) },
                  })
                }
              >
                {remove.isPending && <Spinner />}
                {t('common.remove')}
              </Button>
            </>
          ) : (
            <>
              <Button variant="ghost" className="me-auto text-coral-500" onClick={() => setConfirming(true)}>
                <Trash2 aria-hidden />
                {t('common.remove')}
              </Button>
              {isImage && (
                <Button
                  variant="sun"
                  disabled={describe.isPending}
                  onClick={() =>
                    describe.mutate(
                      { id: shown.id ?? '', force: Boolean(shown.describedAt) },
                      { onSuccess: (updated) => { setCurrent(updated); toast.success(t('media.described')) }, onError: (e) => toast.error(e) },
                    )
                  }
                >
                  {describe.isPending ? <Spinner /> : <Sparkles aria-hidden />}
                  {shown.describedAt ? t('media.describeAgain') : t('media.describe')}
                </Button>
              )}
            </>
          )
        ) : undefined
      }
    >
      <div className="grid gap-5 sm:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]">
        <div>
          {isImage ? (
            <MediaImage contentUrl={shown.contentUrl} mimeType={shown.mimeType} alt={shown.visualDescription ?? t('media.image')} className="aspect-square w-full rounded-2xl" />
          ) : isVideo && file.data ? (
            <video src={file.data} controls className="aspect-square w-full rounded-2xl bg-black" />
          ) : (
            <div className="grid aspect-square w-full place-items-center rounded-2xl bg-surface-2 text-muted">
              <FileText className="size-10" aria-hidden />
            </div>
          )}
          {!isImage && file.data && (
            <a href={file.data} download className={buttonVariants({ variant: 'secondary', size: 'sm', className: 'mt-3' })}>
              <Download aria-hidden />
              {t('media.download')}
            </a>
          )}
        </div>
        <dl className="space-y-3 text-sm">
          <div className="flex flex-wrap gap-x-4 gap-y-1 text-muted">
            <span>{formatBytes(shown.byteSize, i18n.language)}</span>
            {shown.width && shown.height && <span dir="ltr">{shown.width}×{shown.height}</span>}
            <span>{formatDate(shown.createdAt, i18n.language)}</span>
          </div>
          {product && (
            <div>
              <dt className="font-semibold">{t('media.product')}</dt>
              <dd className="text-muted">{product.name}</dd>
            </div>
          )}
          {isImage && !shown.describedAt && <Alert tone="info" title={t('media.notDescribed')}>{t('media.notDescribedHint')}</Alert>}
          {shown.visualDescription && (
            <div>
              <dt className="font-semibold">{t('media.whatAiSees')}</dt>
              <dd className="mt-1 text-muted" dir="auto">{shown.visualDescription}</dd>
            </div>
          )}
          {shown.describedAt && shown.showsProduct != null && (
            <Badge tone={shown.showsProduct ? 'green' : 'sun'}>{shown.showsProduct ? t('media.showsProduct') : t('media.noProductVisible')}</Badge>
          )}
          {shown.describedAt && (
            issues.length === 0 ? (
              <Alert tone="success" title={t('media.goodQuality')} />
            ) : (
              <div>
                <dt className="font-semibold">{t('media.issues')}</dt>
                <dd className="mt-1.5 flex flex-wrap gap-1.5">
                  {issues.map((issue) => <Badge key={issue} tone="coral">{t(`media.issueNames.${issue}`)}</Badge>)}
                </dd>
              </div>
            )
          )}
          {shown.qualityAdvice && (
            <div className="rounded-xl bg-sun-300/20 p-3">
              <dt className="font-semibold">{t('media.advice')}</dt>
              <dd className="mt-1" dir="auto">{shown.qualityAdvice}</dd>
            </div>
          )}
        </dl>
      </div>
    </Dialog>
  )
}
