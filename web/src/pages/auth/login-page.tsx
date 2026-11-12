import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { Link, useLocation, useNavigate } from 'react-router'
import { z } from 'zod'
import { ApiError } from '@/api/errors'
import { login } from '@/auth/auth-api'
import { Alert } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Field } from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { PasswordInput } from '@/components/ui/password-input'
import { Spinner } from '@/components/ui/spinner'
import { describeError, type ErrorText } from '@/lib/error-message'
import { AuthLayout } from './auth-layout'

const schema = z.object({
  email: z.string().trim().min(1, 'validation.required').email('validation.email'),
  password: z.string().min(1, 'validation.required'),
})

type Values = z.infer<typeof schema>

export function LoginPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const location = useLocation()
  const [failure, setFailure] = useState<ErrorText | null>(null)
  const { register, handleSubmit, formState } = useForm<Values>({ resolver: zodResolver(schema) })

  const from = (location.state as { from?: string } | null)?.from ?? '/'

  const onSubmit = handleSubmit(async (values) => {
    setFailure(null)
    try {
      await login(values)
      navigate(from, { replace: true })
    } catch (error) {
      setFailure(
        error instanceof ApiError && error.status === 401
          ? { title: t('auth.login.wrong'), detail: null }
          : describeError(error, t),
      )
    }
  })

  return (
    <AuthLayout title={t('auth.login.title')} subtitle={t('auth.login.subtitle')}>
      <form onSubmit={onSubmit} className="space-y-5" noValidate>
        {failure && <Alert title={failure.title}>{failure.detail}</Alert>}
        <Field label={t('auth.email')} error={formState.errors.email?.message}>
          <Input type="email" autoComplete="email" dir="ltr" {...register('email')} />
        </Field>
        <Field label={t('auth.password')} error={formState.errors.password?.message}>
          <PasswordInput autoComplete="current-password" {...register('password')} />
        </Field>
        <Button type="submit" size="lg" className="w-full" disabled={formState.isSubmitting}>
          {formState.isSubmitting && <Spinner />}
          {t('auth.login.submit')}
        </Button>
        <p className="text-center text-sm text-muted">
          {t('auth.login.noAccount')}{' '}
          <Link to="/register" className="font-semibold text-brand-600 hover:underline dark:text-brand-400">
            {t('auth.login.toRegister')}
          </Link>
        </p>
      </form>
    </AuthLayout>
  )
}
