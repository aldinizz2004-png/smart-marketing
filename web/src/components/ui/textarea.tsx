import type { TextareaHTMLAttributes } from 'react'
import { cn } from '@/lib/cn'
import { fieldControl } from './input'

export function Textarea({ className, rows = 4, ...props }: TextareaHTMLAttributes<HTMLTextAreaElement>) {
  return <textarea rows={rows} className={cn(fieldControl, 'py-2.5 leading-relaxed', className)} {...props} />
}
