import { cn } from '@/lib/cn'

type FilterChipsProps<T extends string> = {
  label: string
  options: { value: T; label: string }[]
  value: T
  onChange: (value: T) => void
}

export function FilterChips<T extends string>({ label, options, value, onChange }: FilterChipsProps<T>) {
  return (
    <div role="group" aria-label={label} className="-mx-1 flex gap-2 overflow-x-auto px-1 pb-1">
      {options.map((option) => (
        <button
          key={option.value}
          type="button"
          aria-pressed={value === option.value}
          onClick={() => onChange(option.value)}
          className={cn(
            'shrink-0 rounded-full border px-3.5 py-1.5 text-sm font-semibold transition-colors',
            value === option.value
              ? 'border-transparent bg-linear-to-br from-brand-600 to-brand-500 text-white shadow-md shadow-brand-600/20'
              : 'border-line bg-surface text-muted hover:text-ink',
          )}
        >
          {option.label}
        </button>
      ))}
    </div>
  )
}
