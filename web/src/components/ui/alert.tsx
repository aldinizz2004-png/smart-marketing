import { CircleAlert, CircleCheck, Info } from 'lucide-react'
import type { ReactNode } from 'react'
import { cn } from '@/lib/cn'

const tones = {
  error: { box: 'border-coral-400/40 bg-coral-400/10 text-coral-500', icon: CircleAlert },
  success: { box: 'border-brand-400/40 bg-brand-400/10 text-brand-700 dark:text-brand-300', icon: CircleCheck },
  info: { box: 'border-grape-400/40 bg-grape-400/10 text-grape-500', icon: Info },
}

type AlertProps = { tone?: keyof typeof tones; title: string; children?: ReactNode; className?: string }

export function Alert({ tone = 'error', title, children, className }: AlertProps) {
  const { box, icon: Icon } = tones[tone]
  return (
    <div role={tone === 'error' ? 'alert' : 'status'} className={cn('flex gap-3 rounded-xl border p-3.5', box, className)}>
      <Icon className="mt-0.5 size-5 shrink-0" aria-hidden />
      <div className="min-w-0 space-y-0.5">
        <p className="font-semibold">{title}</p>
        {children && <div className="text-sm text-ink/80 [overflow-wrap:anywhere]">{children}</div>}
      </div>
    </div>
  )
}
