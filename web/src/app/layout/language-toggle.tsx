import { Languages } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { changeLanguage } from '@/i18n'

export function LanguageToggle() {
  const { t, i18n } = useTranslation()
  return (
    <Button
      variant="ghost"
      size="sm"
      onClick={() => void changeLanguage(i18n.language === 'ar' ? 'en' : 'ar')}
      aria-label={t('actions.switchLanguageLabel')}
    >
      <Languages aria-hidden />
      {t('actions.switchLanguage')}
    </Button>
  )
}
