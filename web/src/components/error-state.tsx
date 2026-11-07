import { RefreshCw, TriangleAlert } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { describeError } from '@/lib/error-message'
import { Button } from './ui/button'

export function ErrorState({ error, onRetry }: { error: unknown; onRetry?: () => void }) {
  const { t } = useTranslation()
  const { title, detail } = describeError(error, t)
  return (
    <div role="alert" className="flex flex-col items-center rounded-3xl border border-coral-400/30 bg-coral-400/5 px-6 py-12 text-center">
      <span className="mb-3 grid size-12 place-items-center rounded-2xl bg-coral-400/15 text-coral-500">
        <TriangleAlert className="size-6" aria-hidden />
      </span>
      <p className="font-bold">{title}</p>
      {detail && <p className="mt-1 max-w-md text-sm text-muted [overflow-wrap:anywhere]">{detail}</p>}
      {onRetry && (
        <Button variant="secondary" size="sm" className="mt-5" onClick={onRetry}>
          <RefreshCw aria-hidden />
          {t('common.retry')}
        </Button>
      )}
    </div>
  )
}
