import type { LucideIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { EmptyState } from '@/components/empty-state'
import { PageHeader } from '@/components/page-header'

type ComingSoonPageProps = {
  page: 'comments' | 'settings'
  icon: LucideIcon
}

// Placeholder until each page gets its own branch.
export function ComingSoonPage({ page, icon }: ComingSoonPageProps) {
  const { t } = useTranslation()
  return (
    <>
      <PageHeader title={t(`pages.${page}.title`)} subtitle={t(`pages.${page}.subtitle`)} />
      <EmptyState icon={icon} title={t('pages.soon')} hint={t('pages.soonHint')} />
    </>
  )
}
