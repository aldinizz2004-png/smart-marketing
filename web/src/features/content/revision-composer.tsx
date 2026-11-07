import { PenLine, Sparkles } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { Field } from '@/components/ui/field'
import { Select } from '@/components/ui/select'
import { Spinner } from '@/components/ui/spinner'
import { Textarea } from '@/components/ui/textarea'
import { useOffers, useProducts } from '@/features/catalog/api'
import type { MediaAsset } from '@/features/media/api'
import { MediaPicker } from '@/features/media/media-picker'
import { cn } from '@/lib/cn'
import { callsToAction, contentFormats, hookTypes, type Revision, type RevisionRequest } from './api'
import { enumLabel } from './labels'
import { formatForImages, mediaProblem } from './media-rules'

type Mode = 'ai' | 'manual'

type RevisionComposerProps = {
  businessId: string
  submitLabel: string
  onSubmit: (request: RevisionRequest) => Promise<unknown>
  base?: Revision | null
  showInstructions?: boolean
}

// One form for both ways of writing a post: the AI writes it, or the owner does.
export function RevisionComposer({ businessId, submitLabel, onSubmit, base, showInstructions }: RevisionComposerProps) {
  const { t } = useTranslation()
  const products = useProducts(businessId, { activeOnly: true })
  const offers = useOffers(businessId, { status: 'Active' })
  const [mode, setMode] = useState<Mode>('ai')
  const [productId, setProductId] = useState('')
  const [offerId, setOfferId] = useState('')
  const [instructions, setInstructions] = useState('')
  const [text, setText] = useState(base?.body ?? '')
  // Keep the base format only when it is valid as it stands; otherwise start from what the images allow.
  const baseUsable = base?.format != null && !mediaProblem(base.format, base.assets?.length ?? 0)
  const [format, setFormat] = useState<string>(baseUsable ? base!.format! : formatForImages(base?.assets?.length ?? 0))
  const [formatTouched, setFormatTouched] = useState(baseUsable)
  const [hook, setHook] = useState<string>(base?.hookType ?? '')
  const [cta, setCta] = useState<string>(base?.callToAction ?? 'OrderNow')
  const [assets, setAssets] = useState<MediaAsset[]>(
    (base?.assets ?? []).map((a) => ({ id: a.assetId, contentUrl: a.contentUrl, mimeType: a.mimeType })),
  )
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [mediaError, setMediaError] = useState<string | null>(null)

  function changeAssets(next: MediaAsset[]) {
    setAssets(next)
    setMediaError(null)
    if (!formatTouched) setFormat(formatForImages(next.length))
  }

  async function submit(event: FormEvent) {
    event.preventDefault()
    if (mode === 'manual' && text.trim() === '') {
      setError('validation.required')
      return
    }
    const problem = mode === 'manual' ? mediaProblem(format as RevisionRequest['format'], assets.length) : null
    if (problem) {
      setMediaError(t(`content.media.${problem.key}`, { format: enumLabel(t, 'format', format), min: problem.min, max: problem.max }))
      return
    }
    setError(null)
    setBusy(true)
    const assetIds = assets.map((a) => a.id!).filter(Boolean)
    try {
      await onSubmit(
        mode === 'ai'
          ? {
              generateWithAi: true,
              productId: productId || null,
              offerId: offerId || null,
              instructions: instructions.trim() || null,
              assetIds,
            }
          : {
              generateWithAi: false,
              text: text.trim(),
              format: format as RevisionRequest['format'],
              hookType: (hook || null) as RevisionRequest['hookType'],
              callToAction: cta as RevisionRequest['callToAction'],
              includesOffer: Boolean(offerId),
              assetIds,
            },
      )
    } finally {
      setBusy(false)
    }
  }

  return (
    <form onSubmit={(e) => void submit(e)} className="space-y-5" noValidate>
      <div role="radiogroup" aria-label={t('content.howToWrite')} className="grid grid-cols-2 gap-2">
        {(['ai', 'manual'] as const).map((value) => {
          const Icon = value === 'ai' ? Sparkles : PenLine
          return (
            <button
              key={value}
              type="button"
              role="radio"
              aria-checked={mode === value}
              onClick={() => setMode(value)}
              className={cn(
                'flex items-center gap-3 rounded-2xl border p-4 text-start transition-colors',
                mode === value ? 'border-brand-500 bg-brand-50 dark:bg-brand-900/40' : 'border-line hover:bg-surface-2',
              )}
            >
              <span className={cn('grid size-10 shrink-0 place-items-center rounded-xl text-white', value === 'ai' ? 'bg-linear-to-br from-grape-400 to-coral-400' : 'bg-linear-to-br from-brand-500 to-brand-700')}>
                <Icon className="size-5" aria-hidden />
              </span>
              <span>
                <span className="block font-semibold">{t(`content.mode.${value}`)}</span>
                <span className="block text-xs text-muted">{t(`content.mode.${value}Hint`)}</span>
              </span>
            </button>
          )
        })}
      </div>

      {mode === 'ai' ? (
        <div className="grid gap-4 sm:grid-cols-2">
          <Field label={t('content.product')} optional>
            <Select value={productId} onChange={(e) => setProductId(e.target.value)}>
              <option value="">{t('content.noProduct')}</option>
              {(products.data?.items ?? []).map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
            </Select>
          </Field>
          <Field label={t('content.offer')} optional>
            <Select value={offerId} onChange={(e) => setOfferId(e.target.value)}>
              <option value="">{t('content.noOffer')}</option>
              {(offers.data?.items ?? []).map((o) => <option key={o.id} value={o.id}>{o.title}</option>)}
            </Select>
          </Field>
          {(showInstructions ?? true) && (
            <div className="sm:col-span-2">
              <Field label={t('content.instructions')} optional hint={t('content.instructionsHint')}>
                <Textarea rows={3} maxLength={500} value={instructions} onChange={(e) => setInstructions(e.target.value)} placeholder={t('content.instructionsPlaceholder')} />
              </Field>
            </div>
          )}
        </div>
      ) : (
        <div className="space-y-4">
          <Field label={t('content.text')} error={error ?? undefined}>
            <Textarea rows={7} maxLength={5000} dir="auto" value={text} onChange={(e) => setText(e.target.value)} />
          </Field>
          <div className="grid gap-4 sm:grid-cols-3">
            <Field label={t('content.format')}>
              <Select
                value={format}
                onChange={(e) => {
                  setFormat(e.target.value)
                  setFormatTouched(true)
                  setMediaError(null)
                }}
              >
                {contentFormats.map((f) => <option key={f} value={f}>{enumLabel(t, 'format', f)}</option>)}
              </Select>
            </Field>
            <Field label={t('content.hook')} optional>
              <Select value={hook} onChange={(e) => setHook(e.target.value)}>
                <option value="">—</option>
                {hookTypes.map((h) => <option key={h} value={h}>{enumLabel(t, 'hook', h)}</option>)}
              </Select>
            </Field>
            <Field label={t('content.cta')}>
              <Select value={cta} onChange={(e) => setCta(e.target.value)}>
                {callsToAction.map((c) => <option key={c} value={c}>{enumLabel(t, 'cta', c)}</option>)}
              </Select>
            </Field>
          </div>
        </div>
      )}

      <MediaPicker businessId={businessId} selected={assets} onChange={changeAssets} />
      {mediaError && <p role="alert" className="text-sm text-coral-500">{mediaError}</p>}

      <Button type="submit" size="lg" variant={mode === 'ai' ? 'sun' : 'primary'} disabled={busy}>
        {busy ? <Spinner /> : mode === 'ai' ? <Sparkles aria-hidden /> : <PenLine aria-hidden />}
        {busy && mode === 'ai' ? t('content.aiWriting') : submitLabel}
      </Button>
    </form>
  )
}
