import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { can } from '@/auth/roles'
import { useSession } from '@/auth/use-session'
import { Button } from '@/components/ui/button'
import { Dialog } from '@/components/ui/dialog'
import { Field } from '@/components/ui/field'
import { Select } from '@/components/ui/select'
import { Spinner } from '@/components/ui/spinner'
import { useToast } from '@/components/ui/toast-context'
import { useWorkspace } from '@/features/workspace/workspace-context'
import { reportDays, useGenerateReport } from './api'

export function GenerateReportDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const { t } = useTranslation()
  const toast = useToast()
  const navigate = useNavigate()
  const session = useSession()
  const { businessId } = useWorkspace()
  const generate = useGenerateReport(businessId)
  const [days, setDays] = useState<number>(7)
  const [send, setSend] = useState(false)
  const canSend = can(session.tenant.role, 'admin')

  const submit = () =>
    generate.mutate(
      { days, send: canSend && send },
      {
        onSuccess: (report) => {
          toast.success(t('reports.generated'))
          onClose()
          if (report.id) void navigate(`/reports/${report.id}`)
        },
        onError: (e) => toast.error(e),
      },
    )

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={t('reports.generateTitle')}
      description={t('reports.generateHint')}
      footer={
        <>
          <Button variant="ghost" onClick={onClose}>{t('common.cancel')}</Button>
          <Button onClick={submit} disabled={generate.isPending}>
            {generate.isPending && <Spinner />}
            {t('reports.generate')}
          </Button>
        </>
      }
    >
      <div className="space-y-4">
        <Field label={t('reports.period')}>
          <Select value={String(days)} onChange={(e) => setDays(Number(e.target.value))}>
            {reportDays.map((d) => <option key={d} value={d}>{t('reports.lastDays', { count: d })}</option>)}
          </Select>
        </Field>
        {canSend && (
          <label className="flex items-center gap-2 text-sm font-semibold">
            <input type="checkbox" className="size-4 accent-brand-600" checked={send} onChange={(e) => setSend(e.target.checked)} />
            {t('reports.alsoSend')}
          </label>
        )}
      </div>
    </Dialog>
  )
}
