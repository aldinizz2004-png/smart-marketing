import { Bot, Database, Monitor, Server, ShieldCheck, type LucideIcon } from 'lucide-react'
import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

const groups: { key: string; icon: LucideIcon; items: string[] }[] = [
  { key: 'backend', icon: Server, items: ['C#', 'ASP.NET Core 10', 'EF Core 10', 'JWT', 'Serilog', 'Swagger'] },
  { key: 'data', icon: Database, items: ['SQL Server 2025', 'EF Core Migrations'] },
  { key: 'frontend', icon: Monitor, items: ['React 19', 'TypeScript', 'Vite', 'Tailwind CSS', 'TanStack Query', 'i18next'] },
  { key: 'ai', icon: Bot, items: ['LLM API', 'Meta Graph API', 'WhatsApp Cloud API'] },
  { key: 'quality', icon: ShieldCheck, items: ['xUnit', 'Vitest', 'Playwright', 'Docker', 'GitHub Actions'] },
]

export function TechStackSection({ action }: { action?: ReactNode }) {
  const { t } = useTranslation()
  return (
    <section id="stack" aria-labelledby="stack-title" className="scroll-mt-20 bg-surface-2/60 py-20">
      <div className="mx-auto max-w-6xl px-4 sm:px-6">
        <header className="flex flex-col gap-6 sm:flex-row sm:items-end sm:justify-between">
          <div className="max-w-2xl">
            <p className="font-semibold text-brand-600 dark:text-brand-400">{t('landing.stack.eyebrow')}</p>
            <h2 id="stack-title" className="mt-2 text-3xl font-extrabold text-balance sm:text-4xl">{t('landing.stack.title')}</h2>
            <p className="mt-4 text-muted">{t('landing.stack.subtitle')}</p>
          </div>
          {action}
        </header>
        <ul className="mt-12 grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
          {groups.map(({ key, icon: Icon, items }) => (
            <li key={key} className="rounded-3xl border border-line bg-surface p-6">
              <h3 className="flex items-center gap-3 font-bold">
                <span className="grid size-10 place-items-center rounded-xl bg-brand-100 text-brand-700 dark:bg-brand-900/60 dark:text-brand-300">
                  <Icon className="size-5" aria-hidden />
                </span>
                {t(`landing.stack.groups.${key}`)}
              </h3>
              <ul className="mt-4 flex flex-wrap gap-2" aria-label={t(`landing.stack.groups.${key}`)}>
                {items.map((item) => (
                  <li key={item} dir="ltr" className="rounded-full border border-line bg-surface-2 px-3 py-1 text-sm font-semibold">
                    {item}
                  </li>
                ))}
              </ul>
            </li>
          ))}
        </ul>
      </div>
    </section>
  )
}
