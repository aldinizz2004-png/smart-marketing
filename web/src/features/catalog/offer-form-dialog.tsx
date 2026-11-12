import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import { Dialog } from '@/components/ui/dialog'
import { Field } from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { Spinner } from '@/components/ui/spinner'
import { Textarea } from '@/components/ui/textarea'
import { useToast } from '@/components/ui/toast-context'
import { useWorkspace } from '@/features/workspace/workspace-context'
import { cn } from '@/lib/cn'
import { fromDateTimeInput, toDateTimeInput } from '@/lib/dates'
import { currencies } from '@/lib/options'
import { useSaveOffer, type Offer } from './api'

const schema = z
  .object({
    title: z.string().trim().min(1, 'validation.required').max(200, 'validation.tooLong'),
    description: z.string().trim().max(2000, 'validation.tooLong'),
    kind: z.enum(['price', 'discount']),
    value: z.string().trim().regex(/^\d+(\.\d{1,2})?$/, 'validation.number'),
    currency: z.string(),
    startsAt: z.string().min(1, 'validation.required'),
    endsAt: z.string().min(1, 'validation.required'),
  })
  .refine((v) => v.kind === 'price' || (Number(v.value) > 0 && Number(v.value) <= 100), { path: ['value'], message: 'validation.percent' })
  .refine((v) => !v.startsAt || !v.endsAt || v.endsAt > v.startsAt, { path: ['endsAt'], message: 'validation.endBeforeStart' })

type Values = z.infer<typeof schema>

type OfferFormDialogProps = { open: boolean; onClose: () => void; offer?: Offer }

// Starts now (to the minute) and runs for a week, ending at midnight.
function defaultWindow() {
  const start = new Date()
  start.setSeconds(0, 0)
  const end = new Date(start)
  end.setDate(end.getDate() + 7)
  end.setHours(23, 59, 0, 0)
  return { start: start.toISOString(), end: end.toISOString() }
}

export function OfferFormDialog({ open, onClose, offer }: OfferFormDialogProps) {
  const { t } = useTranslation()
  const toast = useToast()
  const { businessId, business } = useWorkspace()
  const save = useSaveOffer(businessId)
  const [defaults] = useState(defaultWindow)
  const { register, handleSubmit, formState, reset, control, setValue } = useForm<Values>({
    resolver: zodResolver(schema),
    values: {
      title: offer?.title ?? '',
      description: offer?.description ?? '',
      kind: offer?.discountPercent != null ? 'discount' : 'price',
      value: offer?.discountPercent != null ? String(offer.discountPercent) : offer?.offerPrice != null ? String(offer.offerPrice) : '',
      currency: offer?.currency ?? business.defaultCurrency ?? 'ILS',
      startsAt: toDateTimeInput(offer?.startsAt ?? defaults.start),
      endsAt: toDateTimeInput(offer?.endsAt ?? defaults.end),
    },
  })
  const kind = useWatch({ control, name: 'kind' })

  const close = () => {
    reset()
    onClose()
  }

  const submit = handleSubmit(async (v) => {
    try {
      await save.mutateAsync({
        id: offer?.id,
        body: {
          title: v.title,
          description: v.description || null,
          offerPrice: v.kind === 'price' ? Number(v.value) : null,
          currency: v.kind === 'price' ? v.currency : null,
          discountPercent: v.kind === 'discount' ? Number(v.value) : null,
          startsAt: fromDateTimeInput(v.startsAt)!,
          endsAt: fromDateTimeInput(v.endsAt)!,
        },
      })
      toast.success(offer ? t('common.saved') : t('catalog.offerAdded'))
      close()
    } catch (error) {
      toast.error(error)
    }
  })

  return (
    <Dialog
      open={open}
      onClose={close}
      title={offer ? t('catalog.editOffer') : t('catalog.newOffer')}
      description={offer ? undefined : t('catalog.offerHint')}
      footer={
        <>
          <Button variant="ghost" onClick={close}>{t('common.cancel')}</Button>
          <Button onClick={() => void submit()} disabled={formState.isSubmitting}>
            {formState.isSubmitting && <Spinner />}
            {t('common.save')}
          </Button>
        </>
      }
    >
      <form onSubmit={(e) => { e.preventDefault(); void submit() }} className="grid gap-4 sm:grid-cols-2" noValidate>
        <div className="sm:col-span-2">
          <Field label={t('catalog.offerTitle')} error={formState.errors.title?.message}>
            <Input placeholder={t('catalog.offerTitlePlaceholder')} {...register('title')} />
          </Field>
        </div>
        <div className="sm:col-span-2">
          <p className="mb-1.5 text-sm font-semibold">{t('catalog.offerKind')}</p>
          <div role="radiogroup" aria-label={t('catalog.offerKind')} className="flex gap-1 rounded-2xl bg-surface-2 p-1">
            {(['price', 'discount'] as const).map((k) => (
              <button
                key={k}
                type="button"
                role="radio"
                aria-checked={kind === k}
                onClick={() => setValue('kind', k)}
                className={cn('flex-1 rounded-xl px-3 py-2 text-sm font-semibold', kind === k ? 'bg-surface shadow' : 'text-muted')}
              >
                {t(`catalog.kinds.${k}`)}
              </button>
            ))}
          </div>
        </div>
        <Field label={kind === 'price' ? t('catalog.offerPrice') : t('catalog.discountPercent')} error={formState.errors.value?.message}>
          <Input inputMode="decimal" dir="ltr" {...register('value')} />
        </Field>
        {kind === 'price' ? (
          <Field label={t('catalog.currency')}>
            <Select {...register('currency')}>
              {currencies.map((c) => <option key={c} value={c}>{t(`currencies.${c}`, { defaultValue: c })}</option>)}
            </Select>
          </Field>
        ) : (
          <div />
        )}
        <Field label={t('catalog.startsAt')} error={formState.errors.startsAt?.message}>
          <Input type="datetime-local" dir="ltr" {...register('startsAt')} />
        </Field>
        <Field label={t('catalog.endsAt')} error={formState.errors.endsAt?.message}>
          <Input type="datetime-local" dir="ltr" {...register('endsAt')} />
        </Field>
        <div className="sm:col-span-2">
          <Field label={t('catalog.description')} optional error={formState.errors.description?.message}>
            <Textarea rows={2} {...register('description')} />
          </Field>
        </div>
      </form>
    </Dialog>
  )
}
