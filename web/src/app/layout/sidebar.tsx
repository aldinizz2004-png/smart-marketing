import { useTranslation } from 'react-i18next'
import { NavLink } from 'react-router'
import { cn } from '@/lib/cn'
import { ApiStatus } from './api-status'
import { Brand } from './brand'
import { navItems } from './nav-items'

export function Sidebar() {
  const { t } = useTranslation()
  return (
    <aside className="sticky top-0 hidden h-dvh w-64 shrink-0 flex-col border-e border-line bg-surface p-5 md:flex">
      <Brand />
      <nav aria-label={t('nav.main')} className="mt-8 flex flex-1 flex-col gap-1">
        {navItems.map(({ to, labelKey, icon: Icon }) => (
          <NavLink
            key={to}
            to={to}
            end={to === '/'}
            className={({ isActive }) =>
              cn(
                'flex items-center gap-3 rounded-xl px-3 py-2.5 font-medium transition-colors',
                isActive
                  ? 'bg-linear-to-br from-brand-600 to-brand-500 text-white shadow-md shadow-brand-600/25'
                  : 'text-muted hover:bg-surface-2 hover:text-ink',
              )
            }
          >
            <Icon className="size-5" aria-hidden />
            {t(labelKey)}
          </NavLink>
        ))}
      </nav>
      <ApiStatus />
    </aside>
  )
}
