import { CalendarCheck, Gauge, Hourglass, MessageCircleWarning, Sparkles, type LucideIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { buttonVariants } from '@/components/ui/button-variants'
import { Card } from '@/components/ui/card'
import { cn } from '@/lib/cn'

type Stat = { labelKey: string; icon: LucideIcon; tone: string }

const stats: Stat[] = [
  { labelKey: 'home.stats.publishedToday', icon: CalendarCheck, tone: 'from-brand-500 to-brand-700' },
  { labelKey: 'home.stats.awaitingReview', icon: Hourglass, tone: 'from-sun-400 to-sun-500' },
  { labelKey: 'home.stats.complaints', icon: MessageCircleWarning, tone: 'from-coral-400 to-coral-500' },
  { labelKey: 'home.stats.postsLeft', icon: Gauge, tone: 'from-grape-400 to-grape-500' },
]

export function HomePage() {
  const { t } = useTranslation()

  return (
    <div className="space-y-6">
      <section className="relative overflow-hidden rounded-3xl bg-linear-to-br from-brand-800 via-brand-600 to-brand-400 p-6 text-white shadow-xl shadow-brand-700/25 md:p-10">
        <div className="pointer-events-none absolute -top-16 -end-16 size-56 rounded-full bg-lime-accent/40 blur-3xl" aria-hidden />
        <div className="pointer-events-none absolute -bottom-20 start-1/3 size-56 rounded-full bg-sun-400/30 blur-3xl" aria-hidden />
        <div className="relative max-w-xl">
          <h1 className="text-3xl font-bold md:text-4xl">{t('home.greeting')}</h1>
          <p className="mt-3 text-lg text-brand-50/90">{t('home.intro')}</p>
          <Link to="/content" className={cn(buttonVariants({ variant: 'sun', size: 'lg' }), 'mt-6')}>
            <Sparkles aria-hidden />
            {t('actions.createPost')}
          </Link>
        </div>
      </section>

      <section className="grid grid-cols-2 gap-3 md:grid-cols-4 md:gap-4">
        {stats.map(({ labelKey, icon: Icon, tone }) => (
          <Card key={labelKey} className="flex flex-col gap-3">
            <span className={cn('grid size-11 place-items-center rounded-xl bg-linear-to-br text-white shadow-md', tone)}>
              <Icon className="size-5" aria-hidden />
            </span>
            <p className="text-3xl font-bold" aria-hidden>
              —
            </p>
            <p className="text-sm text-muted">{t(labelKey)}</p>
          </Card>
        ))}
      </section>
    </div>
  )
}
