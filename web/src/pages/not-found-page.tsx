import { SearchX } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { EmptyState } from '@/components/empty-state'
import { buttonVariants } from '@/components/ui/button-variants'

export function NotFoundPage() {
  const { t } = useTranslation()
  return (
    <EmptyState
      icon={SearchX}
      title={t('notFound.title')}
      hint={t('notFound.hint')}
      action={
        <Link to="/" className={buttonVariants()}>
          {t('actions.backHome')}
        </Link>
      }
    />
  )
}
