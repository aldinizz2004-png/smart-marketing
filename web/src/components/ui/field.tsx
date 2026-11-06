import { cloneElement, isValidElement, useId, type ReactElement } from 'react'
import { useTranslation } from 'react-i18next'

type FieldProps = {
  label: string
  error?: string
  hint?: string
  optional?: boolean
  children: ReactElement<{ id?: string; 'aria-invalid'?: boolean; 'aria-describedby'?: string }>
}

// Label + control + hint/error, wired for screen readers. Error strings are translation keys or plain text.
export function Field({ label, error, hint, optional, children }: FieldProps) {
  const { t } = useTranslation()
  const id = useId()
  const messageId = `${id}-message`
  const message = error ? t(error) : hint

  const control = isValidElement(children)
    ? cloneElement(children, {
        id,
        'aria-invalid': error ? true : undefined,
        'aria-describedby': message ? messageId : undefined,
      })
    : children

  return (
    <div className="space-y-1.5">
      <label htmlFor={id} className="flex items-baseline gap-2 text-sm font-semibold">
        {label}
        {optional && <span className="text-xs font-normal text-muted">{t('common.optional')}</span>}
      </label>
      {control}
      {message && (
        <p id={messageId} className={error ? 'text-sm text-coral-500' : 'text-xs text-muted'}>
          {message}
        </p>
      )}
    </div>
  )
}
