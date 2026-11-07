import { cva } from 'class-variance-authority'

export const buttonVariants = cva(
  'inline-flex items-center justify-center gap-2 rounded-xl font-semibold transition-all disabled:pointer-events-none disabled:opacity-50 [&_svg]:size-4 [&_svg]:shrink-0',
  {
    variants: {
      variant: {
        primary:
          'bg-linear-to-br from-brand-600 to-brand-500 text-white shadow-lg shadow-brand-600/25 hover:-translate-y-0.5 hover:shadow-xl hover:shadow-brand-600/30',
        sun: 'bg-sun-400 text-brand-950 shadow-lg shadow-sun-500/25 hover:-translate-y-0.5 hover:bg-sun-300',
        secondary: 'border border-line bg-surface text-ink hover:bg-surface-2',
        ghost: 'text-muted hover:bg-surface-2 hover:text-ink',
        danger: 'bg-coral-500 text-white shadow-lg shadow-coral-500/25 hover:bg-coral-400',
        soft: 'bg-brand-100 text-brand-800 hover:bg-brand-200 dark:bg-brand-900/60 dark:text-brand-200 dark:hover:bg-brand-900',
      },
      size: {
        sm: 'h-9 px-3 text-sm',
        md: 'h-11 px-5 text-sm',
        lg: 'h-12 px-6 text-base',
        icon: 'size-10',
      },
    },
    defaultVariants: { variant: 'primary', size: 'md' },
  },
)
