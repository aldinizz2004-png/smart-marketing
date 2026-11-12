import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router'
import { z } from 'zod'
import { register as registerAccount } from '@/auth/auth-api'
import { Alert } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Field } from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { PasswordInput } from '@/components/ui/password-input'
import { Spinner } from '@/components/ui/spinner'
import type { Language } from '@/i18n'
import { describeError, type ErrorText } from '@/lib/error-message'
import { AuthLayout } from './auth-layout'

// Same rules as the API's RegisterRequest, so most mistakes are caught before sending.
const schema = z.object({
  fullName: z.string().trim().min(1, 'validation.required').max(200, 'validation.tooLong'),
  tenantName: z.string().trim().min(1, 'validation.required').max(200, 'validation.tooLong'),
  email: z.string().trim().min(1, 'validation.required').email('validation.email').max(256, 'validation.tooLong'),
  password: z
    .string()
    .min(8, 'validation.passwordLength')
    .max(100, 'validation.tooLong')
    .regex(/^(?=.*[A-Za-z])(?=.*\d).+$/, 'validation.passwordMix'),
})

type Values = z.infer<typeof schema>

export function RegisterPage() {
  const { t, i18n } = useTranslation()
  const navigate = useNavigate()
  const [failure, setFailure] = useState<ErrorText | null>(null)
  const { register, handleSubmit, formState } = useForm<Values>({ resolver: zodResolver(schema) })

  const onSubmit = handleSubmit(async (values) => {
    setFailure(null)
    try {
      await registerAccount({ ...values, preferredLanguage: i18n.language as Language })
      navigate('/onboarding', { replace: true })
    } catch (error) {
      setFailure(describeError(error, t))
    }
  })

  return (
    <AuthLayout title={t('auth.register.title')} subtitle={t('auth.register.subtitle')}>
      <form onSubmit={onSubmit} className="space-y-5" noValidate>
        {failure && <Alert title={failure.title}>{failure.detail}</Alert>}
        <Field label={t('auth.fullName')} error={formState.errors.fullName?.message}>
          <Input autoComplete="name" {...register('fullName')} />
        </Field>
        <Field label={t('auth.tenantName')} hint={t('auth.tenantNameHint')} error={formState.errors.tenantName?.message}>
          <Input autoComplete="organization" {...register('tenantName')} />
        </Field>
        <Field label={t('auth.email')} error={formState.errors.email?.message}>
          <Input type="email" autoComplete="email" dir="ltr" {...register('email')} />
        </Field>
        <Field label={t('auth.password')} hint={t('auth.passwordHint')} error={formState.errors.password?.message}>
          <PasswordInput autoComplete="new-password" {...register('password')} />
        </Field>
        <Button type="submit" size="lg" className="w-full" disabled={formState.isSubmitting}>
          {formState.isSubmitting && <Spinner />}
          {t('auth.register.submit')}
        </Button>
        <p className="text-center text-sm text-muted">
          {t('auth.register.haveAccount')}{' '}
          <Link to="/login" className="font-semibold text-brand-600 hover:underline dark:text-brand-400">
            {t('auth.register.toLogin')}
          </Link>
        </p>
      </form>
    </AuthLayout>
  )
}
