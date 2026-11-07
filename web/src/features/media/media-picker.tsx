import { Check, ImagePlus, Upload } from 'lucide-react'
import { useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { Dialog } from '@/components/ui/dialog'
import { SectionSpinner, Spinner } from '@/components/ui/spinner'
import { useToast } from '@/components/ui/toast-context'
import { cn } from '@/lib/cn'
import { maxUploadBytes, useMedia, useUploadMedia, type MediaAsset } from './api'
import { MediaImage } from './media-image'

type MediaPickerProps = {
  businessId: string
  selected: MediaAsset[]
  onChange: (assets: MediaAsset[]) => void
  max?: number
}

// Pick up to `max` images for a post; new photos can be uploaded right here.
export function MediaPicker({ businessId, selected, onChange, max = 10 }: MediaPickerProps) {
  const { t } = useTranslation()
  const [open, setOpen] = useState(false)
  return (
    <div className="space-y-2">
      <p className="text-sm font-semibold">{t('content.images')}</p>
      <div className="flex flex-wrap gap-2">
        {selected.map((asset) => (
          <div key={asset.id} className="relative">
            <MediaImage contentUrl={asset.contentUrl} mimeType={asset.mimeType} alt={asset.visualDescription ?? t('content.postImage')} className="size-20 rounded-xl" />
            <button
              type="button"
              onClick={() => onChange(selected.filter((a) => a.id !== asset.id))}
              className="absolute -top-2 -end-2 grid size-6 place-items-center rounded-full bg-coral-500 text-xs font-bold text-white shadow"
              aria-label={t('common.remove')}
            >
              ×
            </button>
          </div>
        ))}
        {selected.length < max && (
          <button
            type="button"
            onClick={() => setOpen(true)}
            className="grid size-20 place-items-center rounded-xl border-2 border-dashed border-line text-muted hover:border-brand-400 hover:text-brand-600"
          >
            <ImagePlus className="size-6" aria-hidden />
            <span className="sr-only">{t('content.addImages')}</span>
          </button>
        )}
      </div>
      {open && <PickerDialog businessId={businessId} selected={selected} max={max} onClose={() => setOpen(false)} onChange={onChange} />}
    </div>
  )
}

function PickerDialog({ businessId, selected, max, onClose, onChange }: MediaPickerProps & { max: number; onClose: () => void }) {
  const { t } = useTranslation()
  const toast = useToast()
  const media = useMedia(businessId, { pageSize: 60 })
  const upload = useUploadMedia(businessId)
  const input = useRef<HTMLInputElement>(null)
  const [picked, setPicked] = useState<MediaAsset[]>(selected)

  const images = (media.data?.items ?? []).filter((a) => a.mimeType?.startsWith('image/'))
  const toggle = (asset: MediaAsset) =>
    setPicked((current) =>
      current.some((a) => a.id === asset.id) ? current.filter((a) => a.id !== asset.id) : current.length < max ? [...current, asset] : current,
    )

  async function onFiles(files: FileList | null) {
    for (const file of Array.from(files ?? [])) {
      if (file.size > maxUploadBytes) {
        toast.error(new Error(t('media.tooLarge')))
        continue
      }
      try {
        const asset = await upload.mutateAsync({ file, kind: 'ProductPhoto' })
        setPicked((current) => (current.length < max ? [...current, asset] : current))
      } catch (error) {
        toast.error(error)
      }
    }
  }

  return (
    <Dialog
      open
      size="lg"
      onClose={onClose}
      title={t('content.chooseImages')}
      description={t('content.chooseImagesHint', { max })}
      footer={
        <>
          <Button variant="ghost" onClick={onClose}>{t('common.cancel')}</Button>
          <Button onClick={() => { onChange(picked); onClose() }}>{t('content.useImages', { count: picked.length })}</Button>
        </>
      }
    >
      <input ref={input} type="file" accept="image/jpeg,image/png,image/webp" multiple hidden onChange={(e) => void onFiles(e.target.files)} />
      <Button variant="secondary" onClick={() => input.current?.click()} disabled={upload.isPending}>
        {upload.isPending ? <Spinner /> : <Upload aria-hidden />}
        {t('media.upload')}
      </Button>
      {media.isPending ? (
        <SectionSpinner />
      ) : images.length === 0 ? (
        <p className="mt-6 text-center text-sm text-muted">{t('media.empty')}</p>
      ) : (
        <ul className="mt-4 grid grid-cols-3 gap-2 sm:grid-cols-4">
          {images.map((asset) => {
            const index = picked.findIndex((a) => a.id === asset.id)
            return (
              <li key={asset.id}>
                <button
                  type="button"
                  aria-pressed={index >= 0}
                  onClick={() => toggle(asset)}
                  className={cn('relative block w-full overflow-hidden rounded-xl ring-offset-2 ring-offset-surface', index >= 0 && 'ring-3 ring-brand-500')}
                >
                  <MediaImage contentUrl={asset.contentUrl} mimeType={asset.mimeType} alt={asset.visualDescription ?? t('content.postImage')} className="aspect-square w-full" />
                  {index >= 0 && (
                    <span className="absolute top-1.5 end-1.5 grid size-6 place-items-center rounded-full bg-brand-500 text-xs font-bold text-white">
                      {index + 1 > 9 ? <Check className="size-3" /> : index + 1}
                    </span>
                  )}
                </button>
              </li>
            )
          })}
        </ul>
      )}
    </Dialog>
  )
}
