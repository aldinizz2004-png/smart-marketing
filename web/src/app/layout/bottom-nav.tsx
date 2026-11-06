import { useTranslation } from 'react-i18next'
import { NavLink } from 'react-router'
import { cn } from '@/lib/cn'
import { navItems } from './nav-items'

export function BottomNav() {
  const { t } = useTranslation()
  return (
    <nav
      aria-label={t('nav.main')}
      className="fixed inset-x-0 bottom-0 z-20 grid grid-cols-4 border-t border-line bg-surface/95 pb-[env(safe-area-inset-bottom)] backdrop-blur md:hidden"
    >
      {navItems.map(({ to, labelKey, icon: Icon }) => (
        <NavLink
          key={to}
          to={to}
          end={to === '/'}
          className={({ isActive }) =>
            cn(
              'flex flex-col items-center gap-1 py-2.5 text-xs font-medium transition-colors',
              isActive ? 'text-brand-600 dark:text-brand-400' : 'text-muted',
            )
          }
        >
          {({ isActive }) => (
            <>
              <span
                className={cn(
                  'grid h-8 w-14 place-items-center rounded-full transition-colors',
                  isActive && 'bg-brand-100 dark:bg-brand-900',
                )}
              >
                <Icon className="size-5" aria-hidden />
              </span>
              {t(labelKey)}
            </>
          )}
        </NavLink>
      ))}
    </nav>
  )
}
