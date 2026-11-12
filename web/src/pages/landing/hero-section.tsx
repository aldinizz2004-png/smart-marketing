import { ArrowLeft, Heart, MessageCircle, Sparkles } from 'lucide-react'
import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { buttonVariants } from '@/components/ui/button-variants'

export function HeroSection({ secondaryAction }: { secondaryAction?: ReactNode }) {
  const { t } = useTranslation()
  return (
    <section id="top" aria-labelledby="hero-title" className="relative overflow-hidden pt-28 pb-16 sm:pt-36 sm:pb-24">
      <div className="pointer-events-none absolute -top-32 -end-32 size-[28rem] rounded-full bg-brand-400/25 blur-3xl" aria-hidden />
      <div className="pointer-events-none absolute top-40 -start-40 size-96 rounded-full bg-lime-accent/20 blur-3xl" aria-hidden />
      <div className="relative mx-auto grid max-w-6xl items-center gap-12 px-4 sm:px-6 lg:grid-cols-[1.1fr_1fr]">
        <div>
          <p className="inline-flex items-center gap-2 rounded-full bg-brand-100 px-3 py-1 text-sm font-semibold text-brand-800 dark:bg-brand-900/60 dark:text-brand-200">
            <Sparkles className="size-4" aria-hidden />
            {t('landing.hero.eyebrow')}
          </p>
          <h1 id="hero-title" className="mt-5 text-4xl leading-tight font-extrabold text-balance sm:text-5xl">
            {t('landing.hero.title')}
          </h1>
          <p className="mt-5 max-w-xl text-lg leading-relaxed text-muted">{t('landing.hero.description')}</p>
          <div className="mt-8 flex flex-wrap gap-3">
            <Link to="/register" className={buttonVariants({ size: 'lg' })}>
              {t('landing.hero.cta')}
              <ArrowLeft className="ltr:rotate-180" aria-hidden />
            </Link>
            {secondaryAction}
          </div>
          <p className="mt-4 text-sm text-muted">{t('landing.hero.note')}</p>
        </div>

        <figure className="relative mx-auto w-full max-w-sm" aria-labelledby="hero-preview-caption">
          <div className="rotate-2 rounded-3xl border border-line bg-surface p-5 shadow-2xl shadow-brand-900/15">
            <div className="flex items-center gap-3">
              <span className="grid size-10 place-items-center rounded-full bg-linear-to-br from-sun-300 to-coral-400 text-sm font-bold text-white" aria-hidden>
                {t('landing.hero.previewInitial')}
              </span>
              <div>
                <p className="font-bold">{t('landing.hero.previewShop')}</p>
                <p className="text-xs text-muted">{t('landing.hero.previewWhen')}</p>
              </div>
            </div>
            <div className="mt-4 grid aspect-[4/3] place-items-center rounded-2xl bg-linear-to-br from-brand-500 via-brand-400 to-lime-accent">
              <span className="text-6xl" role="img" aria-label={t('landing.hero.previewImage')}>☕</span>
            </div>
            <p className="mt-4 leading-relaxed" dir="auto">{t('landing.hero.previewCaption')}</p>
            <div className="mt-4 flex items-center gap-4 text-sm text-muted">
              <span className="inline-flex items-center gap-1"><Heart className="size-4 text-coral-500" aria-hidden />248</span>
              <span className="inline-flex items-center gap-1"><MessageCircle className="size-4" aria-hidden />31</span>
            </div>
          </div>
          <div className="absolute -bottom-6 -start-4 flex items-center gap-2 rounded-2xl bg-sun-400 px-4 py-3 text-sm font-bold text-brand-950 shadow-xl sm:-start-10">
            <Sparkles className="size-4" aria-hidden />
            {t('landing.hero.previewBadge')}
          </div>
          <figcaption id="hero-preview-caption" className="sr-only">{t('landing.hero.previewLabel')}</figcaption>
        </figure>
      </div>
    </section>
  )
}
