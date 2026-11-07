import { useTranslation } from 'react-i18next'
import { Navigate, useNavigate } from 'react-router'
import { can } from '@/auth/roles'
import { useSession } from '@/auth/use-session'
import { PageHeader } from '@/components/page-header'
import { Card } from '@/components/ui/card'
import { useToast } from '@/components/ui/toast-context'
import { BusinessForm } from '@/features/business/business-form'
import { useCreateBusiness } from '@/features/workspace/api'
import { useWorkspace } from '@/features/workspace/workspace-context'

export function NewBusinessPage() {
  const { t } = useTranslation()
  const session = useSession()
  const toast = useToast()
  const navigate = useNavigate()
  const { selectBusiness } = useWorkspace()
  const create = useCreateBusiness()

  if (!can(session.tenant.role, 'admin')) return <Navigate to="/" replace />

  return (
    <>
      <PageHeader title={t('workspace.addBusiness')} subtitle={t('workspace.addBusinessHint')} />
      <Card className="max-w-3xl p-6">
        <BusinessForm
          submitLabel={t('workspace.create')}
          onSubmit={async (request) => {
            try {
              const business = await create.mutateAsync(request)
              selectBusiness(business.id ?? '')
              toast.success(t('workspace.created', { name: business.name }))
              navigate('/')
            } catch (error) {
              toast.error(error)
            }
          }}
        />
      </Card>
    </>
  )
}
