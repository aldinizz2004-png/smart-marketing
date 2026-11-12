import { Images, Package, Tag } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useSearchParams } from 'react-router'
import { PageHeader } from '@/components/page-header'
import { cn } from '@/lib/cn'
import { MediaTab } from './media-tab'
import { OffersTab } from './offers-tab'
import { ProductsTab } from './products-tab'

const tabs = [
  { value: 'products', icon: Package },
  { value: 'offers', icon: Tag },
  { value: 'media', icon: Images },
] as const

type Tab = (typeof tabs)[number]['value']

export function CatalogPage() {
  const { t } = useTranslation()
  const [params, setParams] = useSearchParams()
  const tab: Tab = tabs.some((x) => x.value === params.get('tab')) ? (params.get('tab') as Tab) : 'products'

  return (
    <>
      <PageHeader title={t('catalog.title')} subtitle={t('catalog.subtitle')} />
      <div role="tablist" aria-label={t('catalog.title')} className="mb-5 flex gap-1 rounded-2xl bg-surface-2 p-1 sm:w-fit">
        {tabs.map(({ value, icon: Icon }) => (
          <button
            key={value}
            role="tab"
            type="button"
            aria-selected={tab === value}
            onClick={() => setParams(value === 'products' ? {} : { tab: value })}
            className={cn('flex flex-1 items-center justify-center gap-2 rounded-xl px-4 py-2 text-sm font-semibold sm:flex-none', tab === value ? 'bg-surface shadow' : 'text-muted')}
          >
            <Icon className="size-4" aria-hidden />
            {t(`catalog.tabs.${value}`)}
          </button>
        ))}
      </div>
      {tab === 'products' ? <ProductsTab /> : tab === 'offers' ? <OffersTab /> : <MediaTab />}
    </>
  )
}
