import { BarChart3, CalendarClock, Megaphone, MessagesSquare, Radar, Sparkles, type LucideIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'

const features: { key: string; icon: LucideIcon; tone: string }[] = [
  { key: 'writing', icon: Sparkles, tone: 'from-brand-500 to-lime-accent' },
  { key: 'publishing', icon: CalendarClock, tone: 'from-sun-400 to-coral-400' },
  { key: 'inbox', icon: MessagesSquare, tone: 'from-grape-400 to-brand-500' },
  { key: 'campaigns', icon: Megaphone, tone: 'from-coral-400 to-grape-500' },
  { key: 'competitors', icon: Radar, tone: 'from-brand-600 to-grape-400' },
  { key: 'reports', icon: BarChart3, tone: 'from-lime-accent to-sun-400' },
]

export function FeaturesSection() {
  const { t } = useTranslation()
  return (
    <section id="features" aria-labelledby="features-title" className="scroll-mt-20 py-20">
      <div className="mx-auto max-w-6xl px-4 sm:px-6">
        <header className="mx-auto max-w-2xl text-center">
          <p className="font-semibold text-brand-600 dark:text-brand-400">{t('landing.features.eyebrow')}</p>
          <h2 id="features-title" className="mt-2 text-3xl font-extrabold text-balance sm:text-4xl">{t('landing.features.title')}</h2>
          <p className="mt-4 text-muted">{t('landing.features.subtitle')}</p>
        </header>
        <ul className="mt-12 grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
          {features.map(({ key, icon: Icon, tone }) => (
            <li key={key}>
              <article className="h-full rounded-3xl border border-line bg-surface p-6 shadow-sm transition hover:-translate-y-1 hover:shadow-lg">
                <span className={`grid size-12 place-items-center rounded-2xl bg-linear-to-br ${tone} text-white shadow-md`}>
                  <Icon className="size-6" aria-hidden />
                </span>
                <h3 className="mt-5 text-lg font-bold">{t(`landing.features.items.${key}.title`)}</h3>
                <p className="mt-2 leading-relaxed text-muted">{t(`landing.features.items.${key}.body`)}</p>
              </article>
            </li>
          ))}
        </ul>
      </div>
    </section>
  )
}
