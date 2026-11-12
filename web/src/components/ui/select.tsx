import { ChevronDown } from 'lucide-react'
import type { SelectHTMLAttributes } from 'react'
import { cn } from '@/lib/cn'
import { fieldControl } from './input'

export function Select({ className, children, ...props }: SelectHTMLAttributes<HTMLSelectElement>) {
  return (
    <div className="relative">
      <select className={cn(fieldControl, 'h-11 appearance-none pe-10', className)} {...props}>
        {children}
      </select>
      <ChevronDown className="pointer-events-none absolute inset-y-0 end-3 my-auto size-4 text-muted" aria-hidden />
    </div>
  )
}
