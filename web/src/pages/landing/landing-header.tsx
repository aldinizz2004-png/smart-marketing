import { Menu, X } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { Brand } from '@/app/layout/brand'
import { LanguageToggle } from '@/app/layout/language-toggle'
import { ThemeToggle } from '@/app/layout/theme-toggle'
import { useSessionState } from '@/auth/use-session'
import { buttonVariants } from '@/components/ui/button-variants'
import { cn } from '@/lib/cn'

export type LandingSection = { id: string; labelKey: string }

export function LandingHeader({ sections }: { sections: LandingSection[] }) {
  const { t } = useTranslation()
  const session = useSessionState()
  const [open, setOpen] = useState(false)
  const signedIn = session.status === 'authenticated'

  return (
    <header className="fixed inset-x-0 top-0 z-30 border-b border-line bg-surface/90 backdrop-blur">
      <nav aria-label={t('landing.nav.label')} className="mx-auto flex h-16 max-w-6xl items-center gap-4 px-4 sm:px-6">
        <a href="#top" className="rounded-xl">
          <Brand compact />
        </a>
        <ul className="ms-6 hidden items-center gap-1 lg:flex">
          {sections.map((s) => (
            <li key={s.id}>
              <a href={`#${s.id}`} className="rounded-lg px-3 py-2 text-sm font-semibold text-muted hover:bg-surface-2 hover:text-ink">
                {t(s.labelKey)}
              </a>
            </li>
          ))}
        </ul>
        <div className="ms-auto flex items-center gap-1">
          <LanguageToggle />
          <ThemeToggle />
          {signedIn ? (
            <Link to="/" className={cn(buttonVariants({ size: 'sm' }), 'hidden sm:inline-flex')}>
              {t('landing.nav.dashboard')}
            </Link>
          ) : (
            <>
              <Link to="/login" className={cn(buttonVariants({ variant: 'ghost', size: 'sm' }), 'hidden sm:inline-flex')}>
                {t('landing.nav.login')}
              </Link>
              <Link to="/register" className={cn(buttonVariants({ size: 'sm' }), 'hidden sm:inline-flex')}>
                {t('landing.nav.start')}
              </Link>
            </>
          )}
          <button
            type="button"
            className="grid size-10 place-items-center rounded-xl text-muted hover:bg-surface-2 lg:hidden"
            aria-expanded={open}
            aria-controls="landing-menu"
            aria-label={open ? t('landing.nav.closeMenu') : t('landing.nav.openMenu')}
            onClick={() => setOpen((v) => !v)}
          >
            {open ? <X className="size-5" aria-hidden /> : <Menu className="size-5" aria-hidden />}
          </button>
        </div>
      </nav>
      <div id="landing-menu" className={cn('border-t border-line bg-surface px-4 pb-4 lg:hidden', !open && 'hidden')}>
        <ul className="flex flex-col py-2">
          {sections.map((s) => (
            <li key={s.id}>
              <a href={`#${s.id}`} onClick={() => setOpen(false)} className="block rounded-lg px-3 py-3 font-semibold hover:bg-surface-2">
                {t(s.labelKey)}
              </a>
            </li>
          ))}
        </ul>
        <div className="flex gap-2 sm:hidden">
          {signedIn ? (
            <Link to="/" className={buttonVariants({ className: 'flex-1' })}>{t('landing.nav.dashboard')}</Link>
          ) : (
            <>
              <Link to="/login" className={buttonVariants({ variant: 'secondary', className: 'flex-1' })}>{t('landing.nav.login')}</Link>
              <Link to="/register" className={buttonVariants({ className: 'flex-1' })}>{t('landing.nav.start')}</Link>
            </>
          )}
        </div>
      </div>
    </header>
  )
}
