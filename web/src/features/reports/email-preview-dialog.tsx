import { useTranslation } from 'react-i18next'
import { ErrorState } from '@/components/error-state'
import { Dialog } from '@/components/ui/dialog'
import { SectionSpinner } from '@/components/ui/spinner'
import { useWorkspace } from '@/features/workspace/workspace-context'
import { useReportHtml } from './api'

export function EmailPreviewDialog({ reportId, open, onClose }: { reportId: string; open: boolean; onClose: () => void }) {
  const { t } = useTranslation()
  const { businessId } = useWorkspace()
  const html = useReportHtml(businessId, reportId, open)

  return (
    <Dialog open={open} onClose={onClose} size="lg" title={t('reports.emailVersion')} description={t('reports.emailVersionHint')}>
      {html.isPending ? (
        <SectionSpinner />
      ) : html.isError ? (
        <ErrorState error={html.error} onRetry={() => void html.refetch()} />
      ) : (
        <iframe title={t('reports.emailVersion')} sandbox="" srcDoc={html.data} className="h-[60dvh] w-full rounded-xl border border-line bg-white" />
      )}
    </Dialog>
  )
}
