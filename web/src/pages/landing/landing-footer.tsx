import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { Brand } from '@/app/layout/brand'
import { useSessionState } from '@/auth/use-session'

export function LandingFooter() {
  const { t } = useTranslation()
  const signedIn = useSessionState().status === 'authenticated'
  return (
    <footer className="border-t border-line bg-surface">
      <div className="mx-auto flex max-w-6xl flex-col gap-6 px-4 py-10 sm:flex-row sm:items-center sm:justify-between sm:px-6">
        <Brand />
        <nav aria-label={t('landing.footer.label')}>
          <ul className="flex flex-wrap gap-x-6 gap-y-2 text-sm font-semibold text-muted">
            {signedIn ? (
              <li><Link to="/" className="hover:text-ink">{t('landing.nav.dashboard')}</Link></li>
            ) : (
              <>
                <li><Link to="/login" className="hover:text-ink">{t('landing.nav.login')}</Link></li>
                <li><Link to="/register" className="hover:text-ink">{t('landing.nav.start')}</Link></li>
              </>
            )}
            <li><a href="#top" className="hover:text-ink">{t('landing.footer.backToTop')}</a></li>
          </ul>
        </nav>
        <p className="text-sm text-muted">{t('landing.footer.rights', { year: new Date().getFullYear() })}</p>
      </div>
    </footer>
  )
}
