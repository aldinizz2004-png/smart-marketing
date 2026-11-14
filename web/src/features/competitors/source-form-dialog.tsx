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
import { useWorkspace } from '@/features/workspace/workspace-context'
import { sourceKinds, useAddTrackedSource } from './api'

const schema = z.object({
  url: z
    .string()
    .trim()
    .min(1, 'validation.required')
    .max(500, 'validation.tooLong')
    .refine((v) => /^https?:\/\/\S+\.\S+/i.test(v), 'competitors.urlInvalid'),
  kind: z.enum(['Competitor', 'Professional', 'Inspiration']),
  displayName: z.string().trim().max(120, 'validation.tooLong'),
  notes: z.string().trim().max(500, 'validation.tooLong'),
})

type Values = z.infer<typeof schema>

const empty: Values = { url: '', kind: 'Competitor', displayName: '', notes: '' }

export function SourceFormDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const { t } = useTranslation()
  const toast = useToast()
  const { businessId } = useWorkspace()
  const add = useAddTrackedSource(businessId)
  const { register, handleSubmit, formState, reset } = useForm<Values>({ resolver: zodResolver(schema), defaultValues: empty })

  const close = () => {
    reset(empty)
    onClose()
  }

  const submit = handleSubmit(async (v) => {
    try {
      await add.mutateAsync({ url: v.url, kind: v.kind, displayName: v.displayName || null, notes: v.notes || null })
      toast.success(t('competitors.added'))
      close()
    } catch (error) {
      toast.error(error)
    }
  })

  return (
    <Dialog
      open={open}
      onClose={close}
      title={t('competitors.newSource')}
      description={t('competitors.newSourceHint')}
      footer={
        <>
          <Button variant="ghost" onClick={close}>{t('common.cancel')}</Button>
          <Button onClick={() => void submit()} disabled={formState.isSubmitting}>
            {formState.isSubmitting && <Spinner />}
            {t('competitors.addSource')}
          </Button>
        </>
      }
    >
      <form onSubmit={(e) => { e.preventDefault(); void submit() }} className="space-y-4" noValidate>
        <Field label={t('competitors.url')} hint={t('competitors.urlHint')} error={formState.errors.url?.message}>
          <Input dir="ltr" inputMode="url" autoComplete="off" placeholder="https://www.instagram.com/shop_name" {...register('url')} />
        </Field>
        <Field label={t('competitors.kind')} error={formState.errors.kind?.message}>
          <Select {...register('kind')}>
            {sourceKinds.map((k) => <option key={k} value={k}>{t(`competitors.kinds.${k}`)}</option>)}
          </Select>
        </Field>
        <Field label={t('competitors.displayName')} optional error={formState.errors.displayName?.message}>
          <Input autoComplete="off" {...register('displayName')} />
        </Field>
        <Field label={t('competitors.notes')} optional hint={t('competitors.notesHint')} error={formState.errors.notes?.message}>
          <Textarea rows={3} {...register('notes')} />
        </Field>
      </form>
    </Dialog>
  )
}
