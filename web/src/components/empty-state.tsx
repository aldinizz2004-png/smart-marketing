import type { LucideIcon } from 'lucide-react'
import type { ReactNode } from 'react'

type EmptyStateProps = {
  icon: LucideIcon
  title: string
  hint?: string
  action?: ReactNode
}

export function EmptyState({ icon: Icon, title, hint, action }: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center rounded-3xl border-2 border-dashed border-line bg-surface px-6 py-16 text-center">
      <span className="mb-4 grid size-16 place-items-center rounded-2xl bg-linear-to-br from-brand-500 to-lime-accent text-white shadow-lg shadow-brand-500/30">
        <Icon className="size-8" aria-hidden />
      </span>
      <h2 className="text-lg font-bold">{title}</h2>
      {hint && <p className="mt-1 max-w-sm text-muted">{hint}</p>}
      {action && <div className="mt-6">{action}</div>}
    </div>
  )
}
