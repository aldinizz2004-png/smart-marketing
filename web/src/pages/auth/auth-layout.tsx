import { BarChart3, MessageCircleHeart, Sparkles } from 'lucide-react'
import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Brand } from '@/app/layout/brand'
import { LanguageToggle } from '@/app/layout/language-toggle'

const highlights = [
  { icon: Sparkles, key: 'auth.highlights.ai' },
  { icon: MessageCircleHeart, key: 'auth.highlights.comments' },
  { icon: BarChart3, key: 'auth.highlights.reports' },
]

export function AuthLayout({ title, subtitle, children }: { title: string; subtitle: string; children: ReactNode }) {
  const { t } = useTranslation()
  return (
    <div className="grid min-h-dvh lg:grid-cols-[1fr_1.1fr]">
      <section className="flex flex-col px-5 py-6 sm:px-10">
        <div className="flex items-center justify-between">
          <Brand />
          <LanguageToggle />
        </div>
        <div className="mx-auto flex w-full max-w-md flex-1 flex-col justify-center py-10">
          <h1 className="text-3xl font-bold">{title}</h1>
          <p className="mt-2 text-muted">{subtitle}</p>
          <div className="mt-8">{children}</div>
        </div>
      </section>

      <aside className="relative hidden overflow-hidden bg-linear-to-br from-brand-800 via-brand-600 to-brand-400 p-12 text-white lg:flex lg:flex-col lg:justify-center">
        <div className="pointer-events-none absolute -top-24 -end-24 size-96 rounded-full bg-lime-accent/40 blur-3xl" aria-hidden />
        <div className="pointer-events-none absolute -bottom-24 start-10 size-80 rounded-full bg-sun-400/30 blur-3xl" aria-hidden />
        <div className="relative max-w-lg">
          <p className="text-4xl font-bold leading-tight">{t('auth.pitch')}</p>
          <ul className="mt-10 space-y-5">
            {highlights.map(({ icon: Icon, key }) => (
              <li key={key} className="flex items-center gap-4 text-lg">
                <span className="grid size-12 shrink-0 place-items-center rounded-2xl bg-white/15 backdrop-blur">
                  <Icon className="size-6" aria-hidden />
                </span>
                {t(key)}
              </li>
            ))}
          </ul>
        </div>
      </aside>
    </div>
  )
}
