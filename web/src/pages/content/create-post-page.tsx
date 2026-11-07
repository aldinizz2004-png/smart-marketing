import { Plus } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Navigate, useNavigate, useSearchParams } from 'react-router'
import { can } from '@/auth/roles'
import { useSession } from '@/auth/use-session'
import { PageHeader } from '@/components/page-header'
import { Alert } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Field } from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { SectionSpinner } from '@/components/ui/spinner'
import { useToast } from '@/components/ui/toast-context'
import { useCampaigns } from '@/features/campaigns/api'
import { CampaignFormDialog } from '@/features/campaigns/campaign-form-dialog'
import { contentTypes, useCreateContentItem, type ContentType } from '@/features/content/api'
import { enumLabel } from '@/features/content/labels'
import { RevisionComposer } from '@/features/content/revision-composer'
import { usePlatforms } from '@/features/platforms/api'
import { useSocialAccounts } from '@/features/social/api'
import { useWorkspace } from '@/features/workspace/workspace-context'
import { cn } from '@/lib/cn'
import { fromDateTimeInput } from '@/lib/dates'

export function CreatePostPage() {
  const { t } = useTranslation()
  const toast = useToast()
  const session = useSession()
  const navigate = useNavigate()
  const [params] = useSearchParams()
  const { businessId } = useWorkspace()
  const campaigns = useCampaigns(businessId)
  const platforms = usePlatforms()
  const accounts = useSocialAccounts(businessId)
  const create = useCreateContentItem(businessId)

  const openCampaigns = (campaigns.data?.items ?? []).filter((c) => !['Completed', 'Canceled'].includes(c.status?.code ?? ''))
  const connected = new Set((accounts.data ?? []).filter((a) => a.status?.code === 'Connected').map((a) => a.platformCode))
  const publishable = (platforms.data ?? []).filter((p) => p.supportsPublishing)

  const [campaignId, setCampaignId] = useState(params.get('campaign') ?? '')
  const [platformCode, setPlatformCode] = useState('')
  const [contentType, setContentType] = useState<ContentType>('Post')
  const [plannedAt, setPlannedAt] = useState('')
  const [newCampaign, setNewCampaign] = useState(false)
  const [missing, setMissing] = useState(false)

  if (!can(session.tenant.role, 'edit')) return <Navigate to="/content" replace />
  if (campaigns.isPending || platforms.isPending) return <SectionSpinner />

  const chosenCampaign = campaignId || openCampaigns[0]?.id || ''
  const chosenPlatform = platformCode || [...connected][0] || publishable[0]?.code || ''

  return (
    <>
      <PageHeader title={t('content.create.title')} subtitle={t('content.create.subtitle')} />
      <div className="grid gap-6 lg:grid-cols-[1fr_1.4fr]">
        <Card className="space-y-5 p-6">
          <h2 className="text-lg font-bold">{t('content.create.where')}</h2>
          {openCampaigns.length === 0 ? (
            <Alert tone="info" title={t('content.create.needCampaign')}>
              <Button size="sm" className="mt-2" onClick={() => setNewCampaign(true)}>
                <Plus aria-hidden />
                {t('campaigns.new')}
              </Button>
            </Alert>
          ) : (
            <div className="space-y-2">
              <Field label={t('content.campaign')} error={missing && !chosenCampaign ? 'validation.required' : undefined}>
                <Select value={chosenCampaign} onChange={(e) => setCampaignId(e.target.value)}>
                  {openCampaigns.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
                </Select>
              </Field>
              <button type="button" onClick={() => setNewCampaign(true)} className="text-sm font-semibold text-brand-600 hover:underline dark:text-brand-400">
                + {t('campaigns.new')}
              </button>
            </div>
          )}
          <Field label={t('content.platform')} hint={connected.has(chosenPlatform) ? undefined : t('content.create.notConnected')}>
            <Select value={chosenPlatform} onChange={(e) => setPlatformCode(e.target.value)}>
              {publishable.map((p) => (
                <option key={p.code} value={p.code ?? ''}>
                  {p.displayName}
                  {connected.has(p.code) ? ` ✓` : ''}
                </option>
              ))}
            </Select>
          </Field>
          <div className="space-y-2">
            <p className="text-sm font-semibold">{t('content.contentType')}</p>
            <div role="radiogroup" aria-label={t('content.contentType')} className="flex flex-wrap gap-2">
              {contentTypes.map((type) => (
                <button
                  key={type}
                  type="button"
                  role="radio"
                  aria-checked={contentType === type}
                  onClick={() => setContentType(type)}
                  className={cn('rounded-xl border px-3 py-2 text-sm font-semibold', contentType === type ? 'border-brand-500 bg-brand-50 text-brand-800 dark:bg-brand-900/40 dark:text-brand-200' : 'border-line text-muted hover:text-ink')}
                >
                  {enumLabel(t, 'contentType', type)}
                </button>
              ))}
            </div>
          </div>
          <Field label={t('content.plannedAt')} optional hint={t('content.plannedAtHint')}>
            <Input type="datetime-local" value={plannedAt} onChange={(e) => setPlannedAt(e.target.value)} />
          </Field>
        </Card>

        <Card className="p-6">
          <h2 className="mb-5 text-lg font-bold">{t('content.create.what')}</h2>
          <RevisionComposer
            businessId={businessId}
            submitLabel={t('content.create.submit')}
            onSubmit={async (firstRevision) => {
              if (!chosenCampaign) {
                setMissing(true)
                return
              }
              try {
                const item = await create.mutateAsync({
                  campaignId: chosenCampaign,
                  platformCode: chosenPlatform,
                  contentType,
                  plannedPublishAt: fromDateTimeInput(plannedAt),
                  firstRevision,
                })
                toast.success(t('content.create.done'))
                navigate(`/content/${item.id}`)
              } catch (error) {
                toast.error(error)
              }
            }}
          />
        </Card>
      </div>
      <CampaignFormDialog open={newCampaign} onClose={() => setNewCampaign(false)} onSaved={(c) => setCampaignId(c.id ?? '')} />
    </>
  )
}
