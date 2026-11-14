import { Bot, CreditCard, Store, Users, Waypoints } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router'
import { PageHeader } from '@/components/page-header'
import { cn } from '@/lib/cn'
import { AccountsTab } from './accounts-tab'
import { AutomationTab } from './automation-tab'
import { BusinessTab } from './business-tab'
import { PlanTab } from './plan-tab'
import { TeamTab } from './team-tab'

const tabs = [
  { value: 'business', icon: Store },
  { value: 'accounts', icon: Waypoints },
  { value: 'automation', icon: Bot },
  { value: 'team', icon: Users },
  { value: 'plan', icon: CreditCard },
] as const

type Tab = (typeof tabs)[number]['value']

export function SettingsPage() {
  const { t } = useTranslation()
  const [params, setParams] = useSearchParams()
  const tab: Tab = tabs.some((x) => x.value === params.get('tab')) ? (params.get('tab') as Tab) : 'business'

  return (
    <>
      <PageHeader title={t('pages.settings.title')} subtitle={t('pages.settings.subtitle')} />
      <div role="tablist" aria-label={t('pages.settings.title')} className="mb-5 flex gap-1 overflow-x-auto rounded-2xl bg-surface-2 p-1 sm:w-fit">
        {tabs.map(({ value, icon: Icon }) => (
          <button
            key={value}
            role="tab"
            type="button"
            aria-selected={tab === value}
            onClick={() => setParams(value === 'business' ? {} : { tab: value })}
            className={cn('flex shrink-0 items-center justify-center gap-2 rounded-xl px-4 py-2 text-sm font-semibold', tab === value ? 'bg-surface shadow' : 'text-muted')}
          >
            <Icon className="size-4" aria-hidden />
            {t(`settings.tabs.${value}`)}
          </button>
        ))}
      </div>
      <div className="max-w-4xl">
        {tab === 'business' ? <BusinessTab /> : tab === 'accounts' ? <AccountsTab /> : tab === 'automation' ? <AutomationTab /> : tab === 'team' ? <TeamTab /> : <PlanTab />}
      </div>
    </>
  )
}
