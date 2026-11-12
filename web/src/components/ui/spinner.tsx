import { LoaderCircle } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { cn } from '@/lib/cn'

export function Spinner({ className }: { className?: string }) {
  return <LoaderCircle className={cn('size-5 animate-spin', className)} aria-hidden />
}

export function FullPageSpinner() {
  const { t } = useTranslation()
  return (
    <div className="grid min-h-dvh place-items-center" role="status" aria-label={t('common.loading')}>
      <Spinner className="size-8 text-brand-500" />
    </div>
  )
}

export function SectionSpinner() {
  const { t } = useTranslation()
  return (
    <div className="grid place-items-center py-16" role="status" aria-label={t('common.loading')}>
      <Spinner className="size-7 text-brand-500" />
    </div>
  )
}
