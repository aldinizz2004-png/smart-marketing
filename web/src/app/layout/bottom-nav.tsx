import { Ellipsis, X } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { NavLink, useLocation } from 'react-router'
import { cn } from '@/lib/cn'
import { navItems } from './nav-items'

const tab = (active: boolean) =>
  cn('flex flex-col items-center gap-1 py-2.5 text-xs font-medium transition-colors', active ? 'text-brand-600 dark:text-brand-400' : 'text-muted')

const pill = (active: boolean) => cn('grid h-8 w-14 place-items-center rounded-full transition-colors', active && 'bg-brand-100 dark:bg-brand-900')

export function BottomNav() {
  const { t } = useTranslation()
  const location = useLocation()
  const [moreOpen, setMoreOpen] = useState(false)
  const bottom = navItems.filter((item) => item.bottom)
  const more = navItems.filter((item) => !item.bottom)
  const moreActive = more.some((item) => location.pathname.startsWith(item.to))

  return (
    <>
      {moreOpen && (
        <div className="fixed inset-0 z-30 bg-brand-950/40 md:hidden" onClick={() => setMoreOpen(false)}>
          <nav
            aria-label={t('nav.more')}
            onClick={(e) => e.stopPropagation()}
            className="absolute inset-x-0 bottom-0 rounded-t-3xl bg-surface p-4 pb-[calc(env(safe-area-inset-bottom)+5rem)] shadow-2xl"
          >
            <div className="mb-3 flex items-center justify-between">
              <p className="font-bold">{t('nav.more')}</p>
              <button type="button" onClick={() => setMoreOpen(false)} aria-label={t('common.close')} className="rounded-lg p-1 text-muted">
                <X className="size-5" aria-hidden />
              </button>
            </div>
            <ul className="grid grid-cols-3 gap-2">
              {more.map(({ to, labelKey, icon: Icon }) => (
                <li key={to}>
                  <NavLink
                    to={to}
                    onClick={() => setMoreOpen(false)}
                    className={({ isActive }) =>
                      cn('flex flex-col items-center gap-2 rounded-2xl p-3 text-center text-sm font-medium', isActive ? 'bg-brand-100 text-brand-800 dark:bg-brand-900 dark:text-brand-200' : 'bg-surface-2')
                    }
                  >
                    <Icon className="size-6" aria-hidden />
                    {t(labelKey)}
                  </NavLink>
                </li>
              ))}
            </ul>
          </nav>
        </div>
      )}
      <nav
        aria-label={t('nav.main')}
        className="fixed inset-x-0 bottom-0 z-30 grid grid-cols-5 border-t border-line bg-surface/95 pb-[env(safe-area-inset-bottom)] backdrop-blur md:hidden"
      >
        {bottom.map(({ to, labelKey, icon: Icon }) => (
          <NavLink key={to} to={to} end={to === '/'} className={({ isActive }) => tab(isActive)}>
            {({ isActive }) => (
              <>
                <span className={pill(isActive)}>
                  <Icon className="size-5" aria-hidden />
                </span>
                {t(labelKey)}
              </>
            )}
          </NavLink>
        ))}
        <button type="button" onClick={() => setMoreOpen((v) => !v)} aria-expanded={moreOpen} className={tab(moreActive || moreOpen)}>
          <span className={pill(moreActive || moreOpen)}>
            <Ellipsis className="size-5" aria-hidden />
          </span>
          {t('nav.more')}
        </button>
      </nav>
    </>
  )
}
