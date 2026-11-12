import { CircleAlert, CircleCheck, X } from 'lucide-react'
import { useCallback, useMemo, useRef, useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { describeError } from '@/lib/error-message'
import { cn } from '@/lib/cn'
import { ToastContext, type ToastApi } from './toast-context'

type Toast = { id: number; tone: 'success' | 'error'; title: string; detail?: string | null }

export function ToastProvider({ children }: { children: ReactNode }) {
  const { t } = useTranslation()
  const [toasts, setToasts] = useState<Toast[]>([])
  const nextId = useRef(1)

  const dismiss = useCallback((id: number) => setToasts((all) => all.filter((toast) => toast.id !== id)), [])
  const push = useCallback(
    (toast: Omit<Toast, 'id'>) => {
      const id = nextId.current++
      setToasts((all) => [...all.slice(-2), { ...toast, id }])
      window.setTimeout(() => dismiss(id), toast.tone === 'error' ? 7000 : 3000)
    },
    [dismiss],
  )

  const api = useMemo<ToastApi>(
    () => ({
      success: (title, detail) => push({ tone: 'success', title, detail }),
      error: (error) => push({ tone: 'error', ...describeError(error, t) }),
    }),
    [push, t],
  )

  return (
    <ToastContext.Provider value={api}>
      {children}
      <div className="pointer-events-none fixed inset-x-0 bottom-20 z-50 flex flex-col items-center gap-2 px-4 md:bottom-6">
        {toasts.map((toast) => {
          const Icon = toast.tone === 'success' ? CircleCheck : CircleAlert
          return (
            <div
              key={toast.id}
              role={toast.tone === 'error' ? 'alert' : 'status'}
              className={cn(
                'pointer-events-auto flex w-full max-w-md items-start gap-3 rounded-2xl p-4 text-white shadow-xl',
                toast.tone === 'success' ? 'bg-brand-700' : 'bg-coral-500',
              )}
            >
              <Icon className="mt-0.5 size-5 shrink-0" aria-hidden />
              <div className="min-w-0 flex-1">
                <p className="font-semibold">{toast.title}</p>
                {toast.detail && <p className="text-sm text-white/85 [overflow-wrap:anywhere]">{toast.detail}</p>}
              </div>
              <button type="button" onClick={() => dismiss(toast.id)} aria-label={t('common.close')}>
                <X className="size-4" aria-hidden />
              </button>
            </div>
          )
        })}
      </div>
    </ToastContext.Provider>
  )
}
