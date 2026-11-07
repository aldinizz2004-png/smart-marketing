import { zodResolver } from '@hookform/resolvers/zod'
import { useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import { Field } from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { Spinner } from '@/components/ui/spinner'
import { Textarea } from '@/components/ui/textarea'
import type { Business, BusinessRequest } from '@/features/workspace/api'
import { browserTimeZone, businessCategories, countries, currencies, timeZones } from '@/lib/options'

const other = '__other'

const schema = z
  .object({
    name: z.string().trim().min(1, 'validation.required').max(200, 'validation.tooLong'),
    category: z.string().min(1, 'validation.required'),
    customCategory: z.string().trim().max(100, 'validation.tooLong'),
    countryCode: z.string(),
    city: z.string().trim().max(100, 'validation.tooLong'),
    address: z.string().trim().max(300, 'validation.tooLong'),
    timeZoneId: z.string().min(1, 'validation.required'),
    defaultCurrency: z.string().regex(/^[A-Z]{3}$/, 'validation.required'),
    contactPhone: z.string().trim().regex(/^$|^\+?[0-9 -]{6,30}$/, 'validation.phone'),
    description: z.string().trim().max(2000, 'validation.tooLong'),
  })
  .refine((v) => v.category !== other || v.customCategory.length > 0, { path: ['customCategory'], message: 'validation.required' })

type Values = z.infer<typeof schema>

function toValues(business?: Business): Values {
  const known = (businessCategories as readonly string[]).includes(business?.category ?? '')
  return {
    name: business?.name ?? '',
    category: business ? (known ? business.category! : other) : '',
    customCategory: business && !known ? (business.category ?? '') : '',
    countryCode: business?.countryCode ?? 'PS',
    city: business?.city ?? '',
    address: business?.address ?? '',
    timeZoneId: business?.timeZoneId ?? browserTimeZone(),
    defaultCurrency: business?.defaultCurrency ?? 'ILS',
    contactPhone: business?.contactPhone ?? '',
    description: business?.description ?? '',
  }
}

const orNull = (value: string) => (value.trim() === '' ? null : value.trim())

type BusinessFormProps = {
  business?: Business
  submitLabel: string
  onSubmit: (request: BusinessRequest) => Promise<unknown>
  readOnly?: boolean
}

export function BusinessForm({ business, submitLabel, onSubmit, readOnly }: BusinessFormProps) {
  const { t } = useTranslation()
  const { register, handleSubmit, control, formState } = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: toValues(business),
  })
  const errors = formState.errors
  const category = useWatch({ control, name: 'category' })

  const submit = handleSubmit((v) =>
    onSubmit({
      name: v.name,
      category: v.category === other ? v.customCategory : v.category,
      countryCode: orNull(v.countryCode),
      city: orNull(v.city),
      address: orNull(v.address),
      timeZoneId: v.timeZoneId,
      defaultCurrency: v.defaultCurrency,
      contactPhone: orNull(v.contactPhone),
      description: orNull(v.description),
    }).catch(() => undefined),
  )

  return (
    <form onSubmit={submit} className="space-y-5" noValidate>
      <fieldset disabled={readOnly || formState.isSubmitting} className="grid gap-5 sm:grid-cols-2">
        <div className="sm:col-span-2">
          <Field label={t('business.name')} error={errors.name?.message}>
            <Input {...register('name')} />
          </Field>
        </div>
        <Field label={t('business.category')} error={errors.category?.message}>
          <Select {...register('category')}>
            <option value="">{t('common.choose')}</option>
            {businessCategories.map((c) => (
              <option key={c} value={c}>{t(`business.categories.${c}`)}</option>
            ))}
            <option value={other}>{t('business.categories.other')}</option>
          </Select>
        </Field>
        {category === other ? (
          <Field label={t('business.customCategory')} error={errors.customCategory?.message}>
            <Input {...register('customCategory')} />
          </Field>
        ) : (
          <Field label={t('business.phone')} optional error={errors.contactPhone?.message}>
            <Input type="tel" dir="ltr" {...register('contactPhone')} />
          </Field>
        )}
        <Field label={t('business.country')} optional>
          <Select {...register('countryCode')}>
            <option value="">—</option>
            {countries.map((c) => (
              <option key={c} value={c}>{t(`countries.${c}`)}</option>
            ))}
          </Select>
        </Field>
        <Field label={t('business.city')} optional error={errors.city?.message}>
          <Input {...register('city')} />
        </Field>
        <Field label={t('business.timeZone')} hint={t('business.timeZoneHint')} error={errors.timeZoneId?.message}>
          <Select dir="ltr" {...register('timeZoneId')}>
            {timeZones.map((z) => (
              <option key={z} value={z}>{z}</option>
            ))}
          </Select>
        </Field>
        <Field label={t('business.currency')} error={errors.defaultCurrency?.message}>
          <Select {...register('defaultCurrency')}>
            {currencies.map((c) => (
              <option key={c} value={c}>{t(`currencies.${c}`)} ({c})</option>
            ))}
          </Select>
        </Field>
        {category === other && (
          <Field label={t('business.phone')} optional error={errors.contactPhone?.message}>
            <Input type="tel" dir="ltr" {...register('contactPhone')} />
          </Field>
        )}
        <div className="sm:col-span-2">
          <Field label={t('business.address')} optional error={errors.address?.message}>
            <Input {...register('address')} />
          </Field>
        </div>
        <div className="sm:col-span-2">
          <Field label={t('business.description')} optional hint={t('business.descriptionHint')} error={errors.description?.message}>
            <Textarea {...register('description')} />
          </Field>
        </div>
      </fieldset>
      {!readOnly && (
        <Button type="submit" size="lg" disabled={formState.isSubmitting}>
          {formState.isSubmitting && <Spinner />}
          {submitLabel}
        </Button>
      )}
    </form>
  )
}
