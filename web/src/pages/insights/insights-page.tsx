import { ChartNoAxesColumn, Lightbulb, Telescope } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router'
import { PageHeader } from '@/components/page-header'
import { cn } from '@/lib/cn'
import { PerformanceTab } from './performance-tab'
import { RecommendationsTab } from './recommendations-tab'
import { RunsTab } from './runs-tab'

const tabs = [
  { value: 'performance', icon: ChartNoAxesColumn },
  { value: 'recommendations', icon: Lightbulb },
  { value: 'runs', icon: Telescope },
] as const

type Tab = (typeof tabs)[number]['value']

export function InsightsPage() {
  const { t } = useTranslation()
  const [params, setParams] = useSearchParams()
  const tab: Tab = tabs.some((x) => x.value === params.get('tab')) ? (params.get('tab') as Tab) : 'performance'
  const days = Number(params.get('days')) || 7

  const changeTab = (value: Tab) => setParams(value === 'performance' ? {} : { tab: value })
  const changeDays = (value: number) => {
    const next = new URLSearchParams(params)
    next.set('days', String(value))
    setParams(next)
  }

  return (
    <>
      <PageHeader title={t('insights.title')} subtitle={t('insights.subtitle')} />
      <div role="tablist" aria-label={t('insights.title')} className="mb-5 flex gap-1 overflow-x-auto rounded-2xl bg-surface-2 p-1 sm:w-fit">
        {tabs.map(({ value, icon: Icon }) => (
          <button
            key={value}
            role="tab"
            type="button"
            aria-selected={tab === value}
            onClick={() => changeTab(value)}
            className={cn('flex flex-1 items-center justify-center gap-2 rounded-xl px-4 py-2 text-sm font-semibold whitespace-nowrap sm:flex-none', tab === value ? 'bg-surface shadow' : 'text-muted')}
          >
            <Icon className="size-4" aria-hidden />
            {t(`insights.tabs.${value}`)}
          </button>
        ))}
      </div>
      {tab === 'performance' ? <PerformanceTab days={days} onDaysChange={changeDays} /> : tab === 'recommendations' ? <RecommendationsTab /> : <RunsTab />}
    </>
  )
}
