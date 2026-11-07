import { zodResolver } from '@hookform/resolvers/zod'
import { Plus, X } from 'lucide-react'
import type { ReactNode } from 'react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import { Field } from '@/components/ui/field'
import { Select } from '@/components/ui/select'
import { Spinner } from '@/components/ui/spinner'
import { Textarea } from '@/components/ui/textarea'
import { cn } from '@/lib/cn'
import { dialects, tones } from '@/lib/options'
import type { BrandProfile, BrandProfileRequest } from './api'

const schema = z.object({
  dialect: z.string().min(1, 'validation.required'),
  tone: z.string().min(1, 'validation.required'),
  emojiLevel: z.number().int().min(0).max(3),
  guidelines: z.string().trim().max(4000, 'validation.tooLong'),
  colorPalette: z.array(z.string()).max(5),
})

type Values = z.infer<typeof schema>

const emojiSamples = ['—', '🙂', '😍✨', '🔥😍🎉']

type BrandProfileFormProps = {
  profile?: BrandProfile | null
  submitLabel: string
  onSubmit: (request: BrandProfileRequest) => Promise<unknown>
  readOnly?: boolean
  secondaryAction?: ReactNode
}

export function BrandProfileForm({ profile, submitLabel, onSubmit, readOnly, secondaryAction }: BrandProfileFormProps) {
  const { t } = useTranslation()
  const { register, handleSubmit, control, formState, setValue } = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: {
      dialect: profile?.dialect ?? 'Palestinian Arabic',
      tone: profile?.tone ?? 'friendly',
      emojiLevel: profile?.emojiLevel ?? 1,
      guidelines: profile?.guidelines ?? '',
      colorPalette: profile?.colorPalette ?? ['#0f7667', '#fbbf24'],
    },
  })
  const palette = useWatch({ control, name: 'colorPalette' })
  const dialect = useWatch({ control, name: 'dialect' })
  const tone = useWatch({ control, name: 'tone' })
  const knownDialect = (dialects as readonly string[]).includes(dialect)
  const knownTone = (tones as readonly string[]).includes(tone)

  const submit = handleSubmit((v) =>
    onSubmit({ ...v, guidelines: v.guidelines.trim() === '' ? null : v.guidelines.trim() }).catch(() => undefined),
  )

  return (
    <form onSubmit={submit} className="space-y-6" noValidate>
      <fieldset disabled={readOnly || formState.isSubmitting} className="space-y-6">
        <div className="grid gap-5 sm:grid-cols-2">
          <Field label={t('brand.dialect')} hint={t('brand.dialectHint')} error={formState.errors.dialect?.message}>
            <Select {...register('dialect')}>
              {!knownDialect && <option value={dialect}>{dialect}</option>}
              {dialects.map((d) => (
                <option key={d} value={d}>{t(`brand.dialects.${d}`)}</option>
              ))}
            </Select>
          </Field>
          <Field label={t('brand.tone')} error={formState.errors.tone?.message}>
            <Select {...register('tone')}>
              {!knownTone && <option value={tone}>{tone}</option>}
              {tones.map((value) => (
                <option key={value} value={value}>{t(`brand.tones.${value}`)}</option>
              ))}
            </Select>
          </Field>
        </div>

        <div className="space-y-2">
          <p className="text-sm font-semibold">{t('brand.emojiLevel')}</p>
          <Controller
            control={control}
            name="emojiLevel"
            render={({ field }) => (
              <div role="radiogroup" aria-label={t('brand.emojiLevel')} className="grid grid-cols-4 gap-2">
                {emojiSamples.map((sample, level) => (
                  <button
                    key={level}
                    type="button"
                    role="radio"
                    aria-checked={field.value === level}
                    onClick={() => field.onChange(level)}
                    className={cn(
                      'rounded-xl border p-3 text-center transition-colors',
                      field.value === level ? 'border-brand-500 bg-brand-50 dark:bg-brand-900/40' : 'border-line hover:bg-surface-2',
                    )}
                  >
                    <span className="block text-lg">{sample}</span>
                    <span className="block text-xs text-muted">{t(`brand.emoji.${level}`)}</span>
                  </button>
                ))}
              </div>
            )}
          />
        </div>

        <div className="space-y-2">
          <p className="text-sm font-semibold">{t('brand.colors')}</p>
          <div className="flex flex-wrap items-center gap-2">
            {palette.map((color, index) => (
              <div key={index} className="flex items-center gap-1 rounded-xl border border-line p-1 pe-2">
                <input
                  type="color"
                  value={color}
                  aria-label={t('brand.colorN', { n: index + 1 })}
                  onChange={(e) => setValue('colorPalette', palette.map((c, i) => (i === index ? e.target.value : c)))}
                  className="size-8 cursor-pointer rounded-lg border-0 bg-transparent"
                />
                <span className="font-mono text-xs uppercase" dir="ltr">{color}</span>
                <button
                  type="button"
                  aria-label={t('common.remove')}
                  onClick={() => setValue('colorPalette', palette.filter((_, i) => i !== index))}
                  className="text-muted hover:text-coral-500"
                >
                  <X className="size-4" aria-hidden />
                </button>
              </div>
            ))}
            {palette.length < 5 && (
              <Button type="button" variant="secondary" size="sm" onClick={() => setValue('colorPalette', [...palette, '#8b5cf6'])}>
                <Plus aria-hidden />
                {t('brand.addColor')}
              </Button>
            )}
          </div>
        </div>

        <Field label={t('brand.guidelines')} optional hint={t('brand.guidelinesHint')} error={formState.errors.guidelines?.message}>
          <Textarea rows={5} placeholder={t('brand.guidelinesPlaceholder')} {...register('guidelines')} />
        </Field>
      </fieldset>

      {!readOnly && (
        <div className="flex flex-wrap gap-2">
          <Button type="submit" size="lg" disabled={formState.isSubmitting}>
            {formState.isSubmitting && <Spinner />}
            {submitLabel}
          </Button>
          {secondaryAction}
        </div>
      )}
    </form>
  )
}
