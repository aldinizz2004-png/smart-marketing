import { ChevronLeft, ChevronRight } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Button } from './ui/button'

type PaginationProps = { page: number; totalPages: number; onChange: (page: number) => void }

export function Pagination({ page, totalPages, onChange }: PaginationProps) {
  const { t } = useTranslation()
  if (totalPages <= 1) return null
  return (
    <nav aria-label={t('common.pages')} className="mt-6 flex items-center justify-center gap-3">
      <Button variant="secondary" size="sm" onClick={() => onChange(page - 1)} disabled={page <= 1} aria-label={t('common.previous')}>
        <ChevronRight className="ltr:rotate-180" aria-hidden />
      </Button>
      <span className="text-sm text-muted">{t('common.pageOf', { page, total: totalPages })}</span>
      <Button variant="secondary" size="sm" onClick={() => onChange(page + 1)} disabled={page >= totalPages} aria-label={t('common.next')}>
        <ChevronLeft className="ltr:rotate-180" aria-hidden />
      </Button>
    </nav>
  )
}
