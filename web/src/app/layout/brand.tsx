import { Megaphone } from 'lucide-react'
import { useTranslation } from 'react-i18next'

export function Brand() {
  const { t } = useTranslation()
  return (
    <div className="flex items-center gap-3">
      <span className="grid size-10 place-items-center rounded-xl bg-linear-to-br from-brand-700 via-brand-500 to-lime-accent text-white shadow-md shadow-brand-600/30">
        <Megaphone className="size-5" aria-hidden />
      </span>
      <div className="leading-tight">
        <p className="font-bold">{t('app.name')}</p>
        <p className="text-xs text-muted">{t('app.tagline')}</p>
      </div>
    </div>
  )
}
