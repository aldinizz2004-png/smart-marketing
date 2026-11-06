import { useTranslation } from 'react-i18next'
import { useApiHealth } from '@/api/health'
import { cn } from '@/lib/cn'

export function ApiStatus() {
  const { t } = useTranslation()
  const { isPending, isSuccess } = useApiHealth()
  const label = isPending ? t('api.checking') : isSuccess ? t('api.online') : t('api.offline')

  return (
    <p className="flex items-center gap-2 text-xs text-muted" role="status">
      <span
        className={cn(
          'size-2 rounded-full',
          isPending ? 'animate-pulse bg-sun-400' : isSuccess ? 'bg-brand-500' : 'bg-coral-500',
        )}
        aria-hidden
      />
      {label}
    </p>
  )
}
