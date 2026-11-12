import { zodResolver } from '@hookform/resolvers/zod'
import { ExternalLink, Link2 } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { ApiError } from '@/api/errors'
import { Alert } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Field } from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { PasswordInput } from '@/components/ui/password-input'
import { Select } from '@/components/ui/select'
import { Spinner } from '@/components/ui/spinner'
import { useToast } from '@/components/ui/toast-context'
import { usePlatforms } from '@/features/platforms/api'
import { useConnectLink, useConnectSocialAccount } from './api'

const schema = z.object({
  platformCode: z.string().min(1, 'validation.required'),
  handle: z.string().trim().max(100, 'validation.tooLong'),
  externalAccountId: z.string().trim().min(1, 'validation.required').max(200, 'validation.tooLong'),
  accessToken: z.string().trim().max(4000, 'validation.tooLong'),
})

type Values = z.infer<typeof schema>

// Automatic connection goes through the publishing provider; when it is not set up, the account is added by hand.
export function ConnectAccountForm({ businessId, onConnected }: { businessId: string; onConnected?: () => void }) {
  const { t } = useTranslation()
  const toast = useToast()
  const platforms = usePlatforms()
  const connect = useConnectSocialAccount(businessId)
  const link = useConnectLink(businessId)
  const [autoUnavailable, setAutoUnavailable] = useState(false)
  const { register, handleSubmit, formState, getValues, reset } = useForm<Values>({
    resolver: zodResolver(schema),
    defaultValues: { platformCode: 'instagram', handle: '', externalAccountId: '', accessToken: '' },
  })

  const publishable = (platforms.data ?? []).filter((p) => p.supportsPublishing)
  const platformField = register('platformCode')

  async function connectAutomatically() {
    try {
      const result = await link.mutateAsync(getValues('platformCode'))
      if (result.authorizationUrl) window.open(result.authorizationUrl, '_blank', 'noopener')
    } catch (error) {
      if (error instanceof ApiError && (error.status === 409 || error.status === 400)) setAutoUnavailable(true)
      else toast.error(error)
    }
  }

  const submit = handleSubmit(async (v) => {
    try {
      await connect.mutateAsync({
        platformCode: v.platformCode,
        externalAccountId: v.externalAccountId,
        handle: v.handle.replace(/^@/, '') || null,
        accessToken: v.accessToken || null,
      })
      toast.success(t('social.connected'))
      reset({ ...v, handle: '', externalAccountId: '', accessToken: '' })
      onConnected?.()
    } catch (error) {
      toast.error(error)
    }
  })

  return (
    <form onSubmit={submit} className="space-y-5" noValidate>
      <Field label={t('social.platform')} error={formState.errors.platformCode?.message}>
        <Select
          {...platformField}
          onChange={(e) => {
            setAutoUnavailable(false)
            void platformField.onChange(e)
          }}
        >
          {publishable.map((p) => (
            <option key={p.code} value={p.code ?? ''}>{p.displayName}</option>
          ))}
        </Select>
      </Field>

      <div className="rounded-2xl border border-line bg-surface-2/50 p-4">
        <p className="font-semibold">{t('social.autoTitle')}</p>
        <p className="mt-1 text-sm text-muted">{t('social.autoHint')}</p>
        <Button type="button" variant="secondary" size="sm" className="mt-3" onClick={() => void connectAutomatically()} disabled={link.isPending}>
          {link.isPending ? <Spinner /> : <ExternalLink aria-hidden />}
          {t('social.autoButton')}
        </Button>
        {autoUnavailable && (
          <Alert tone="info" title={t('social.autoUnavailable')} className="mt-3">
            {t('social.autoUnavailableHint')}
          </Alert>
        )}
      </div>

      <div className="space-y-4">
        <p className="font-semibold">{t('social.manualTitle')}</p>
        <div className="grid gap-4 sm:grid-cols-2">
          <Field label={t('social.handle')} optional error={formState.errors.handle?.message}>
            <Input dir="ltr" placeholder="@myshop" {...register('handle')} />
          </Field>
          <Field label={t('social.accountId')} hint={t('social.accountIdHint')} error={formState.errors.externalAccountId?.message}>
            <Input dir="ltr" {...register('externalAccountId')} />
          </Field>
        </div>
        <Field label={t('social.accessToken')} optional hint={t('social.accessTokenHint')} error={formState.errors.accessToken?.message}>
          <PasswordInput autoComplete="off" {...register('accessToken')} />
        </Field>
        <Button type="submit" disabled={formState.isSubmitting}>
          {formState.isSubmitting ? <Spinner /> : <Link2 aria-hidden />}
          {t('social.connect')}
        </Button>
      </div>
    </form>
  )
}
