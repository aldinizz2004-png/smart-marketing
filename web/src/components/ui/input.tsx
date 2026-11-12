import type { InputHTMLAttributes } from 'react'
import { cn } from '@/lib/cn'

export const fieldControl =
  'w-full rounded-xl border border-line bg-surface px-3.5 text-ink placeholder:text-muted/70 transition-colors focus:border-brand-500 focus:outline-none focus:ring-4 focus:ring-brand-500/15 disabled:opacity-60 aria-invalid:border-coral-500 aria-invalid:focus:ring-coral-500/15'

export function Input({ className, ...props }: InputHTMLAttributes<HTMLInputElement>) {
  return <input className={cn(fieldControl, 'h-11', className)} {...props} />
}
