import { Upload } from 'lucide-react'
import { useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { Dialog } from '@/components/ui/dialog'
import { Field } from '@/components/ui/field'
import { Select } from '@/components/ui/select'
import { Spinner } from '@/components/ui/spinner'
import { useToast } from '@/components/ui/toast-context'
import { useProducts } from '@/features/catalog/api'
import { useWorkspace } from '@/features/workspace/workspace-context'
import { formatBytes } from '@/lib/format'
import { checkMediaFile, mediaKinds, mediaRules, useUploadMedia, type MediaKind } from './api'

const uploadKinds = mediaKinds.filter((k) => k !== 'GeneratedImage')

export function UploadDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const { t, i18n } = useTranslation()
  const toast = useToast()
  const { businessId } = useWorkspace()
  const products = useProducts(businessId, { activeOnly: true })
  const upload = useUploadMedia(businessId)
  const input = useRef<HTMLInputElement>(null)
  const [kind, setKind] = useState<MediaKind>('ProductPhoto')
  const [productId, setProductId] = useState('')
  const [files, setFiles] = useState<File[]>([])
  const [error, setError] = useState<string>()

  const close = () => {
    setFiles([])
    setError(undefined)
    onClose()
  }

  const pick = (list: FileList | null) => {
    const picked = Array.from(list ?? [])
    const bad = picked.map((f) => [f, checkMediaFile(f, kind)] as const).find(([, problem]) => problem)
    if (bad) {
      setError(t(bad[1] === 'size' ? 'media.tooBig' : 'media.wrongType', { name: bad[0].name, max: formatBytes(mediaRules[kind].maxBytes, i18n.language) }))
      setFiles([])
    } else {
      setError(undefined)
      setFiles(picked)
    }
  }

  const send = async () => {
    let done = 0
    for (const file of files) {
      try {
        await upload.mutateAsync({ file, kind, productId: productId || null })
        done++
      } catch (e) {
        toast.error(e)
      }
    }
    if (done > 0) toast.success(t('media.uploaded', { count: done }))
    if (done === files.length) close()
  }

  return (
    <Dialog
      open={open}
      onClose={close}
      title={t('media.uploadTitle')}
      footer={
        <>
          <Button variant="ghost" onClick={close}>{t('common.cancel')}</Button>
          <Button onClick={() => void send()} disabled={files.length === 0 || upload.isPending}>
            {upload.isPending ? <Spinner /> : <Upload aria-hidden />}
            {t('media.uploadCount', { count: files.length })}
          </Button>
        </>
      }
    >
      <div className="space-y-4">
        <Field label={t('media.kind')} hint={t(`media.kindHints.${kind}`, { max: formatBytes(mediaRules[kind].maxBytes, i18n.language) })}>
          <Select value={kind} onChange={(e) => { setKind(e.target.value as MediaKind); setFiles([]); setError(undefined) }}>
            {uploadKinds.map((k) => <option key={k} value={k}>{t(`media.kinds.${k}`)}</option>)}
          </Select>
        </Field>
        {(products.data?.items ?? []).length > 0 && (
          <Field label={t('media.product')} optional hint={t('media.productHint')}>
            <Select value={productId} onChange={(e) => setProductId(e.target.value)}>
              <option value="">{t('media.noProduct')}</option>
              {products.data!.items!.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
            </Select>
          </Field>
        )}
        <input ref={input} type="file" multiple accept={mediaRules[kind].accept} hidden onChange={(e) => { pick(e.target.files); e.target.value = '' }} data-testid="media-file" />
        <button
          type="button"
          onClick={() => input.current?.click()}
          className="flex w-full flex-col items-center gap-2 rounded-2xl border-2 border-dashed border-line p-6 text-muted hover:border-brand-400 hover:text-brand-600"
        >
          <Upload className="size-6" aria-hidden />
          <span className="font-semibold">{files.length ? t('media.filesPicked', { count: files.length }) : t('media.pickFiles')}</span>
          {files.length > 0 && <span className="text-xs">{files.map((f) => f.name).join('، ')}</span>}
        </button>
        {error && <p role="alert" className="text-sm text-coral-500">{error}</p>}
      </div>
    </Dialog>
  )
}
