import { CalendarClock, FileText, MailCheck, Plus } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useSearchParams } from 'react-router'
import { can } from '@/auth/roles'
import { useSession } from '@/auth/use-session'
import { EmptyState } from '@/components/empty-state'
import { ErrorState } from '@/components/error-state'
import { PageHeader } from '@/components/page-header'
import { Pagination } from '@/components/pagination'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { SectionSpinner } from '@/components/ui/spinner'
import { useReports } from '@/features/reports/api'
import { GenerateReportDialog } from '@/features/reports/generate-dialog'
import { useWorkspace } from '@/features/workspace/workspace-context'
import { cn } from '@/lib/cn'
import { formatDate } from '@/lib/format'

export function ReportsPage() {
  const { t, i18n } = useTranslation()
  const session = useSession()
  const { businessId } = useWorkspace()
  const [params, setParams] = useSearchParams()
  const page = Number(params.get('page') ?? 1)
  const reports = useReports(businessId, page)
  const [generating, setGenerating] = useState(false)
  const canEdit = can(session.tenant.role, 'edit')
  const items = reports.data?.items ?? []

  return (
    <>
      <PageHeader
        title={t('reports.title')}
        subtitle={t('reports.subtitle')}
        actions={
          canEdit && (
            <Button onClick={() => setGenerating(true)}>
              <Plus aria-hidden />
              {t('reports.new')}
            </Button>
          )
        }
      />
      {reports.isPending ? (
        <SectionSpinner />
      ) : reports.isError ? (
        <ErrorState error={reports.error} onRetry={() => void reports.refetch()} />
      ) : items.length === 0 ? (
        <EmptyState
          icon={FileText}
          title={t('reports.empty')}
          hint={t('reports.emptyHint')}
          action={canEdit && <Button onClick={() => setGenerating(true)}><Plus aria-hidden />{t('reports.new')}</Button>}
        />
      ) : (
        <>
          <ul className={cn('grid gap-4 md:grid-cols-2', reports.isFetching && 'opacity-70')}>
            {items.map((report) => (
              <li key={report.id}>
                <Link to={`/reports/${report.id}`} className="block h-full rounded-2xl border border-line bg-surface p-5 shadow-sm transition-shadow hover:shadow-md">
                  <p className="mb-2 flex items-center gap-2 text-sm text-muted">
                    <CalendarClock className="size-4" aria-hidden />
                    {t('reports.periodRange', { from: formatDate(report.periodStart, i18n.language), to: formatDate(report.periodEnd, i18n.language) })}
                  </p>
                  <h2 dir="auto" className="mb-3 text-lg font-bold [overflow-wrap:anywhere]">{report.headline}</h2>
                  <div className="flex flex-wrap gap-1.5">
                    <Badge tone={report.isScheduled ? 'grape' : 'gray'}>{report.isScheduled ? t('reports.scheduled') : t('reports.manual')}</Badge>
                    {report.sentAt ? (
                      <Badge tone="green"><MailCheck className="size-3.5" aria-hidden />{t('reports.sentOn', { date: formatDate(report.sentAt, i18n.language) })}</Badge>
                    ) : (
                      <Badge tone="sun">{t('reports.notSent')}</Badge>
                    )}
                  </div>
                </Link>
              </li>
            ))}
          </ul>
          <Pagination
            page={page}
            totalPages={reports.data?.totalPages ?? 1}
            onChange={(p) => {
              const next = new URLSearchParams(params)
              next.set('page', String(p))
              setParams(next)
            }}
          />
        </>
      )}
      <GenerateReportDialog open={generating} onClose={() => setGenerating(false)} />
    </>
  )
}
