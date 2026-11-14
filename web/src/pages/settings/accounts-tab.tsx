import { Plus } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { can } from '@/auth/roles'
import { useSession } from '@/auth/use-session'
import { ConfirmDialog } from '@/components/confirm-dialog'
import { ErrorState } from '@/components/error-state'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Dialog } from '@/components/ui/dialog'
import { SectionSpinner } from '@/components/ui/spinner'
import { useToast } from '@/components/ui/toast-context'
import { usePlatformName } from '@/features/platforms/api'
import { useDisconnectSocialAccount, useSocialAccounts, type SocialAccount } from '@/features/social/api'
import { ConnectAccountForm } from '@/features/social/connect-account-form'
import { SocialAccountsList } from '@/features/social/social-accounts-list'
import { useWorkspace } from '@/features/workspace/workspace-context'

export function AccountsTab() {
  const { t } = useTranslation()
  const toast = useToast()
  const session = useSession()
  const platformName = usePlatformName()
  const { businessId } = useWorkspace()
  const accounts = useSocialAccounts(businessId)
  const disconnect = useDisconnectSocialAccount(businessId)
  const [adding, setAdding] = useState(false)
  const [leaving, setLeaving] = useState<SocialAccount | null>(null)
  const canAdmin = can(session.tenant.role, 'admin')

  const confirmDisconnect = () => {
    if (!leaving?.id) return
    disconnect.mutate(leaving.id, {
      onSuccess: () => toast.success(t('settings.accounts.disconnected')),
      onError: (e) => toast.error(e),
      onSettled: () => setLeaving(null),
    })
  }

  return (
    <Card className="space-y-4 p-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h2 className="text-xl font-bold">{t('settings.accounts.title')}</h2>
          <p className="mt-1 text-sm text-muted">{canAdmin ? t('settings.accounts.subtitle') : t('settings.accounts.adminOnly')}</p>
        </div>
        {canAdmin && (
          <Button onClick={() => setAdding(true)}>
            <Plus aria-hidden />
            {t('settings.accounts.add')}
          </Button>
        )}
      </div>

      {accounts.isPending ? (
        <SectionSpinner />
      ) : accounts.isError ? (
        <ErrorState error={accounts.error} onRetry={() => void accounts.refetch()} />
      ) : (
        <SocialAccountsList accounts={accounts.data ?? []} onDisconnect={canAdmin ? setLeaving : undefined} />
      )}

      <Dialog open={adding} onClose={() => setAdding(false)} title={t('settings.accounts.add')} size="lg">
        <ConnectAccountForm
          businessId={businessId}
          onConnected={() => {
            setAdding(false)
          }}
        />
      </Dialog>
      <ConfirmDialog
        open={!!leaving}
        danger
        busy={disconnect.isPending}
        title={t('settings.accounts.disconnectTitle', { name: leaving ? platformName(leaving.platformCode) : '' })}
        description={t('settings.accounts.disconnectHint')}
        confirmLabel={t('social.disconnect')}
        onConfirm={confirmDisconnect}
        onClose={() => setLeaving(null)}
      />
    </Card>
  )
}
