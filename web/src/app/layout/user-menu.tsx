import { useQueryClient } from '@tanstack/react-query'
import { ChevronDown, LogOut, UserRound } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { logout } from '@/auth/auth-api'
import { useSession } from '@/auth/use-session'
import { initials } from '@/lib/initials'

export function UserMenu() {
  const { t } = useTranslation()
  const session = useSession()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [open, setOpen] = useState(false)
  const box = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!open) return
    const close = (event: MouseEvent | KeyboardEvent) => {
      if (event instanceof KeyboardEvent ? event.key === 'Escape' : !box.current?.contains(event.target as Node)) setOpen(false)
    }
    document.addEventListener('mousedown', close)
    document.addEventListener('keydown', close)
    return () => {
      document.removeEventListener('mousedown', close)
      document.removeEventListener('keydown', close)
    }
  }, [open])

  const name = session.user.fullName || session.user.email

  async function signOut() {
    setOpen(false)
    await logout().catch(() => undefined)
    queryClient.clear()
    navigate('/login', { replace: true })
  }

  return (
    <div ref={box} className="relative">
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        aria-expanded={open}
        aria-haspopup="menu"
        className="flex items-center gap-2 rounded-xl p-1 pe-2 hover:bg-surface-2"
      >
        <span className="grid size-9 place-items-center rounded-xl bg-linear-to-br from-grape-400 to-coral-400 text-sm font-bold text-white">
          {initials(name)}
        </span>
        <span className="hidden max-w-32 truncate text-sm font-semibold sm:block">{name}</span>
        <ChevronDown className="size-4 text-muted" aria-hidden />
      </button>

      {open && (
        <div role="menu" className="absolute end-0 top-full z-30 mt-2 w-64 rounded-2xl border border-line bg-surface p-2 shadow-xl">
          <div className="px-3 py-2">
            <p className="truncate font-semibold">{name}</p>
            <p className="truncate text-xs text-muted" dir="ltr">
              {session.user.email}
            </p>
            <p className="mt-1 text-xs text-muted">
              {session.tenant.name} · {t(`roles.${session.tenant.role}`)}
            </p>
          </div>
          <div className="my-1 border-t border-line" />
          <button
            type="button"
            role="menuitem"
            onClick={() => {
              setOpen(false)
              navigate('/settings/profile')
            }}
            className="flex w-full items-center gap-2 rounded-xl px-3 py-2 text-start text-sm hover:bg-surface-2"
          >
            <UserRound className="size-4" aria-hidden />
            {t('nav.profile')}
          </button>
          <button
            type="button"
            role="menuitem"
            onClick={() => void signOut()}
            className="flex w-full items-center gap-2 rounded-xl px-3 py-2 text-start text-sm text-coral-500 hover:bg-coral-400/10"
          >
            <LogOut className="size-4" aria-hidden />
            {t('auth.logout')}
          </button>
        </div>
      )}
    </div>
  )
}
