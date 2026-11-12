import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
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
import { useProducts } from '@/features/catalog/api'
import { enumLabel } from '@/features/content/labels'
import { useWorkspace } from '@/features/workspace/workspace-context'
import { fromDateInput, toDateInput } from '@/lib/dates'
import { objectives, useSaveCampaign, type Campaign, type CampaignObjective } from './api'

const schema = z
  .object({
    name: z.string().trim().min(1, 'validation.required').max(200, 'validation.tooLong'),
    objective: z.string().min(1, 'validation.required'),
    goalStatement: z.string().trim().min(1, 'validation.required').max(1000, 'validation.tooLong'),
    budget: z.string().regex(/^$|^\d+(\.\d{1,2})?$/, 'validation.number'),
    startsAt: z.string(),
    endsAt: z.string(),
    productIds: z.array(z.string()),
  })
  .refine((v) => !v.startsAt || !v.endsAt || v.endsAt >= v.startsAt, { path: ['endsAt'], message: 'validation.endBeforeStart' })

type Values = z.infer<typeof schema>

type CampaignFormDialogProps = { open: boolean; onClose: () => void; campaign?: Campaign; onSaved?: (campaign: Campaign) => void }

export function CampaignFormDialog({ open, onClose, campaign, onSaved }: CampaignFormDialogProps) {
  const { t } = useTranslation()
  const toast = useToast()
  const { businessId, business } = useWorkspace()
  const products = useProducts(businessId, { activeOnly: true })
  const save = useSaveCampaign(businessId)
  const { register, handleSubmit, formState, reset } = useForm<Values>({
    resolver: zodResolver(schema),
    values: {
      name: campaign?.name ?? '',
      objective: campaign?.objective ?? 'Orders',
      goalStatement: campaign?.goalStatement ?? '',
      budget: campaign?.budget != null ? String(campaign.budget) : '',
      startsAt: toDateInput(campaign?.startsAt),
      endsAt: toDateInput(campaign?.endsAt),
      productIds: campaign?.productIds ?? [],
    },
  })

  const close = () => {
    reset()
    onClose()
  }

  const submit = handleSubmit(async (v) => {
    try {
      const saved = await save.mutateAsync({
        id: campaign?.id,
        body: {
          name: v.name,
          objective: v.objective as CampaignObjective,
          goalStatement: v.goalStatement,
          budget: v.budget ? Number(v.budget) : null,
          budgetCurrency: v.budget ? business.defaultCurrency : null,
          startsAt: fromDateInput(v.startsAt),
          endsAt: fromDateInput(v.endsAt, true),
          productIds: v.productIds,
          offerIds: campaign?.offerIds ?? [],
          audienceSegmentIds: campaign?.audienceSegmentIds ?? [],
          targetMetric: campaign?.targetMetric ?? null,
          targetValue: campaign?.targetValue ?? null,
        },
      })
      toast.success(campaign ? t('common.saved') : t('campaigns.created'))
      onSaved?.(saved)
      close()
    } catch (error) {
      toast.error(error)
    }
  })

  return (
    <Dialog
      open={open}
      onClose={close}
      size="lg"
      title={campaign ? t('campaigns.edit') : t('campaigns.new')}
      description={t('campaigns.formHint')}
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
          <Field label={t('campaigns.name')} error={formState.errors.name?.message}>
            <Input placeholder={t('campaigns.namePlaceholder')} {...register('name')} />
          </Field>
        </div>
        <Field label={t('campaigns.objective')} error={formState.errors.objective?.message}>
          <Select {...register('objective')}>
            {objectives.map((o) => <option key={o} value={o}>{enumLabel(t, 'objective', o)}</option>)}
          </Select>
        </Field>
        <Field label={t('campaigns.budget')} optional hint={business.defaultCurrency ?? undefined} error={formState.errors.budget?.message}>
          <Input inputMode="decimal" dir="ltr" {...register('budget')} />
        </Field>
        <div className="sm:col-span-2">
          <Field label={t('campaigns.goal')} hint={t('campaigns.goalHint')} error={formState.errors.goalStatement?.message}>
            <Textarea rows={3} {...register('goalStatement')} />
          </Field>
        </div>
        <Field label={t('campaigns.startsAt')} optional>
          <Input type="date" {...register('startsAt')} />
        </Field>
        <Field label={t('campaigns.endsAt')} optional error={formState.errors.endsAt?.message}>
          <Input type="date" {...register('endsAt')} />
        </Field>
        {(products.data?.items ?? []).length > 0 && (
          <fieldset className="sm:col-span-2">
            <legend className="mb-2 text-sm font-semibold">{t('campaigns.products')}</legend>
            <div className="flex flex-wrap gap-2">
              {products.data!.items!.map((p) => (
                <label key={p.id} className="flex cursor-pointer items-center gap-2 rounded-xl border border-line px-3 py-2 text-sm has-checked:border-brand-500 has-checked:bg-brand-50 dark:has-checked:bg-brand-900/40">
                  <input type="checkbox" value={p.id} className="accent-brand-600" {...register('productIds')} />
                  {p.name}
                </label>
              ))}
            </div>
          </fieldset>
        )}
      </form>
    </Dialog>
  )
}
