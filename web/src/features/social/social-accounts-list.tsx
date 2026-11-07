import { AtSign, Unplug } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { StatusBadge } from '@/components/status-badge'
import { Button } from '@/components/ui/button'
import { usePlatformName } from '@/features/platforms/api'
import type { SocialAccount } from './api'

type SocialAccountsListProps = {
  accounts: SocialAccount[]
  onDisconnect?: (account: SocialAccount) => void
}

export function SocialAccountsList({ accounts, onDisconnect }: SocialAccountsListProps) {
  const { t } = useTranslation()
  const platformName = usePlatformName()

  if (accounts.length === 0) return <p className="text-sm text-muted">{t('social.none')}</p>

  return (
    <ul className="divide-y divide-line rounded-2xl border border-line">
      {accounts.map((account) => (
        <li key={account.id} className="flex flex-wrap items-center gap-3 p-4">
          <span className="grid size-10 place-items-center rounded-xl bg-linear-to-br from-grape-400 to-coral-400 text-white">
            <AtSign className="size-5" aria-hidden />
          </span>
          <div className="min-w-0 flex-1">
            <p className="font-semibold">{platformName(account.platformCode)}</p>
            <p className="truncate text-sm text-muted" dir="ltr">
              {account.handle ? `@${account.handle}` : account.externalAccountId}
            </p>
          </div>
          <StatusBadge status={account.status} />
          {onDisconnect && account.status?.code === 'Connected' && (
            <Button variant="ghost" size="sm" onClick={() => onDisconnect(account)}>
              <Unplug aria-hidden />
              {t('social.disconnect')}
            </Button>
          )}
        </li>
      ))}
    </ul>
  )
}
