import { Languages, Moon, Sun } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { changeLanguage } from '@/i18n'
import { useTheme } from '@/lib/theme'
import { Brand } from './brand'

export function TopBar() {
  const { t, i18n } = useTranslation()
  const { theme, toggle } = useTheme()

  return (
    <header className="sticky top-0 z-10 flex items-center justify-between gap-3 border-b border-line bg-bg/85 px-4 py-3 backdrop-blur md:justify-end md:px-8">
      <div className="md:hidden">
        <Brand />
      </div>
      <div className="flex items-center gap-1">
        <Button
          variant="ghost"
          size="sm"
          onClick={() => void changeLanguage(i18n.language === 'ar' ? 'en' : 'ar')}
          aria-label={t('actions.switchLanguageLabel')}
        >
          <Languages aria-hidden />
          {t('actions.switchLanguage')}
        </Button>
        <Button
          variant="ghost"
          size="icon"
          onClick={toggle}
          aria-label={theme === 'dark' ? t('actions.lightMode') : t('actions.darkMode')}
        >
          {theme === 'dark' ? <Sun aria-hidden /> : <Moon aria-hidden />}
        </Button>
      </div>
    </header>
  )
}
