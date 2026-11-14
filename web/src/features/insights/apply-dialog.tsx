import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { Button } from '@/components/ui/button'
import { Dialog } from '@/components/ui/dialog'
import { Field } from '@/components/ui/field'
import { Select } from '@/components/ui/select'
import { Spinner } from '@/components/ui/spinner'
import { useToast } from '@/components/ui/toast-context'
import { useCampaigns } from '@/features/campaigns/api'
import { useWorkspace } from '@/features/workspace/workspace-context'
import { useApplyRecommendation, type Recommendation } from './api'

const closedCampaigns = ['Completed', 'Canceled']

// A recommendation that is not tied to a campaign needs one before the AI can write the post.
export function ApplyDialog({ recommendation, onClose }: { recommendation: Recommendation | null; onClose: () => void }) {
  const { t } = useTranslation()
  const toast = useToast()
  const navigate = useNavigate()
  const { businessId } = useWorkspace()
  const campaigns = useCampaigns(businessId)
  const apply = useApplyRecommendation(businessId)
  const [campaignId, setCampaignId] = useState('')
  const needsCampaign = !recommendation?.campaignId
  const open = campaigns.data?.items?.filter((c) => !closedCampaigns.includes(c.status?.code ?? '')) ?? []

  const close = () => {
    setCampaignId('')
    onClose()
  }

  const submit = () => {
    if (!recommendation?.id) return
    apply.mutate(
      { id: recommendation.id, campaignId: needsCampaign ? campaignId : undefined },
      {
        onSuccess: (item) => {
          toast.success(t('insights.recommendations.applied'))
          close()
          if (item.id) void navigate(`/content/${item.id}`)
        },
        onError: (e) => toast.error(e),
      },
    )
  }

  return (
    <Dialog
      open={!!recommendation}
      onClose={close}
      title={t('insights.recommendations.applyTitle')}
      description={needsCampaign ? t('insights.recommendations.applyHint') : t('insights.recommendations.applyHintCampaign')}
      footer={
        <>
          <Button variant="ghost" onClick={close}>{t('common.cancel')}</Button>
          <Button onClick={submit} disabled={apply.isPending || (needsCampaign && !campaignId)}>
            {apply.isPending && <Spinner />}
            {t('insights.recommendations.apply')}
          </Button>
        </>
      }
    >
      {!needsCampaign ? null : open.length === 0 && !campaigns.isPending ? (
        <p className="text-muted">{t('insights.recommendations.noCampaign')}</p>
      ) : (
        <Field label={t('insights.recommendations.campaign')}>
          <Select value={campaignId} onChange={(e) => setCampaignId(e.target.value)}>
            <option value="">{t('common.choose')}</option>
            {open.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
          </Select>
        </Field>
      )}
    </Dialog>
  )
}
