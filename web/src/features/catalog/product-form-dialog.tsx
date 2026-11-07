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
import { currencies } from '@/lib/options'
import { useSaveProduct, type Product } from './api'

const schema = z.object({
  name: z.string().trim().min(1, 'validation.required').max(200, 'validation.tooLong'),
  sku: z.string().trim().max(64, 'validation.tooLong'),
  description: z.string().trim().max(2000, 'validation.tooLong'),
  price: z.string().trim().regex(/^\d+(\.\d{1,2})?$/, 'validation.number'),
  currency: z.string().min(1, 'validation.required'),
  isActive: z.boolean(),
})

type Values = z.infer<typeof schema>

type ProductFormDialogProps = { open: boolean; onClose: () => void; product?: Product }

export function ProductFormDialog({ open, onClose, product }: ProductFormDialogProps) {
  const { t } = useTranslation()
  const toast = useToast()
  const { businessId, business } = useWorkspace()
  const save = useSaveProduct(businessId)
  const { register, handleSubmit, formState, reset } = useForm<Values>({
    resolver: zodResolver(schema),
    values: {
      name: product?.name ?? '',
      sku: product?.sku ?? '',
      description: product?.description ?? '',
      price: product?.price != null ? String(product.price) : '',
      currency: product?.currency ?? business.defaultCurrency ?? 'ILS',
      isActive: product?.isActive ?? true,
    },
  })

  const close = () => {
    reset()
    onClose()
  }

  const submit = handleSubmit(async (v) => {
    try {
      await save.mutateAsync({
        id: product?.id,
        body: { name: v.name, sku: v.sku || null, description: v.description || null, price: Number(v.price), currency: v.currency, isActive: v.isActive },
      })
      toast.success(product ? t('common.saved') : t('catalog.productAdded'))
      close()
    } catch (error) {
      toast.error(error)
    }
  })

  return (
    <Dialog
      open={open}
      onClose={close}
      title={product ? t('catalog.editProduct') : t('catalog.newProduct')}
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
          <Field label={t('catalog.productName')} error={formState.errors.name?.message}>
            <Input placeholder={t('catalog.productNamePlaceholder')} {...register('name')} />
          </Field>
        </div>
        <Field label={t('catalog.price')} error={formState.errors.price?.message}>
          <Input inputMode="decimal" dir="ltr" {...register('price')} />
        </Field>
        <Field label={t('catalog.currency')} error={formState.errors.currency?.message}>
          <Select {...register('currency')}>
            {currencies.map((c) => <option key={c} value={c}>{t(`currencies.${c}`, { defaultValue: c })}</option>)}
          </Select>
        </Field>
        <div className="sm:col-span-2">
          <Field label={t('catalog.sku')} optional hint={t('catalog.skuHint')} error={formState.errors.sku?.message}>
            <Input dir="ltr" {...register('sku')} />
          </Field>
        </div>
        <div className="sm:col-span-2">
          <Field label={t('catalog.description')} optional hint={t('catalog.descriptionHint')} error={formState.errors.description?.message}>
            <Textarea rows={3} {...register('description')} />
          </Field>
        </div>
        <label className="flex items-center gap-2 text-sm font-semibold sm:col-span-2">
          <input type="checkbox" className="size-4 accent-brand-600" {...register('isActive')} />
          {t('catalog.activeLabel')}
        </label>
      </form>
    </Dialog>
  )
}
