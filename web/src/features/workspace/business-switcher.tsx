import { Check, ChevronsUpDown, Plus, Store } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { can } from '@/auth/roles'
import { useSession } from '@/auth/use-session'
import { useWorkspace } from './workspace-context'

export function BusinessSwitcher({ compact = false }: { compact?: boolean }) {
  const { t } = useTranslation()
  const { businesses, business, selectBusiness } = useWorkspace()
  const session = useSession()
  const navigate = useNavigate()
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

  return (
    <div ref={box} className="relative">
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        aria-expanded={open}
        aria-haspopup="listbox"
        aria-label={t('workspace.switch')}
        className={
          compact
            ? 'flex max-w-44 items-center gap-2 rounded-xl border border-line bg-surface-2/60 px-2.5 py-1.5 text-start text-sm hover:bg-surface-2'
            : 'flex w-full items-center gap-3 rounded-2xl border border-line bg-surface-2/60 p-2.5 text-start hover:bg-surface-2'
        }
      >
        <span
          className={`grid shrink-0 place-items-center rounded-xl bg-linear-to-br from-sun-400 to-coral-400 text-white ${compact ? 'size-6 rounded-lg' : 'size-9'}`}
        >
          <Store className={compact ? 'size-3.5' : 'size-5'} aria-hidden />
        </span>
        <span className="min-w-0 flex-1">
          {!compact && <span className="block text-xs text-muted">{t('workspace.current')}</span>}
          <span className="block truncate font-semibold">{business.name}</span>
        </span>
        <ChevronsUpDown className="size-4 shrink-0 text-muted" aria-hidden />
      </button>

      {open && (
        <div className={`absolute top-full z-30 mt-2 rounded-2xl border border-line bg-surface p-2 shadow-xl ${compact ? 'start-0 w-64' : 'inset-x-0'}`}>
          <ul role="listbox" aria-label={t('workspace.switch')} className="max-h-72 overflow-y-auto">
            {businesses.map((b) => (
              <li key={b.id} role="option" aria-selected={b.id === business.id}>
                <button
                  type="button"
                  onClick={() => {
                    selectBusiness(b.id ?? '')
                    setOpen(false)
                  }}
                  className="flex w-full items-center gap-2 rounded-xl px-3 py-2 text-start text-sm hover:bg-surface-2"
                >
                  <span className="min-w-0 flex-1 truncate">{b.name}</span>
                  {b.id === business.id && <Check className="size-4 text-brand-500" aria-hidden />}
                </button>
              </li>
            ))}
          </ul>
          {can(session.tenant.role, 'admin') && (
            <>
              <div className="my-1 border-t border-line" />
              <button
                type="button"
                onClick={() => {
                  setOpen(false)
                  navigate('/businesses/new')
                }}
                className="flex w-full items-center gap-2 rounded-xl px-3 py-2 text-sm font-semibold text-brand-600 hover:bg-surface-2 dark:text-brand-400"
              >
                <Plus className="size-4" aria-hidden />
                {t('workspace.addBusiness')}
              </button>
            </>
          )}
        </div>
      )}
    </div>
  )
}
