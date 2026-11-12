import type { ReactNode } from 'react'
import { cn } from '@/lib/cn'

const badgeTones = {
  green: 'bg-brand-100 text-brand-800 dark:bg-brand-900 dark:text-brand-200',
  sun: 'bg-sun-300/30 text-amber-800 dark:bg-sun-500/20 dark:text-sun-300',
  coral: 'bg-coral-400/15 text-coral-500 dark:bg-coral-500/20 dark:text-coral-400',
  grape: 'bg-grape-400/15 text-grape-500 dark:bg-grape-500/20 dark:text-grape-400',
  gray: 'bg-surface-2 text-muted',
} as const

export type BadgeTone = keyof typeof badgeTones

export function Badge({ tone = 'gray', children, className }: { tone?: BadgeTone; children: ReactNode; className?: string }) {
  return (
    <span className={cn('inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-semibold whitespace-nowrap', badgeTones[tone], className)}>
      {children}
    </span>
  )
}
