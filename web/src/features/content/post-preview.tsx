import { Heart, MessageCircle, Send } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Badge } from '@/components/ui/badge'
import { usePlatformName } from '@/features/platforms/api'
import { MediaImage } from '@/features/media/media-image'
import { useWorkspace } from '@/features/workspace/workspace-context'
import { initials } from '@/lib/initials'
import type { Revision } from './api'
import { enumLabel } from './labels'

type CreativeDetails = { hashtags?: string[]; visualIdea?: string }

export function PostPreview({ revision, platformCode }: { revision?: Revision | null; platformCode?: string | null }) {
  const { t } = useTranslation()
  const { business } = useWorkspace()
  const platformName = usePlatformName()
  const details = (revision?.creativeDetails ?? {}) as CreativeDetails
  const assets = [...(revision?.assets ?? [])].sort((a, b) => (a.position ?? 0) - (b.position ?? 0))
  const hashtags = (details.hashtags ?? []).filter((tag) => !revision?.body?.includes(tag))

  return (
    <article aria-label={t('content.preview')} className="overflow-hidden rounded-3xl border border-line bg-surface shadow-lg">
      <header className="flex items-center gap-3 p-4">
        <span className="grid size-10 place-items-center rounded-full bg-linear-to-br from-sun-400 to-coral-400 text-sm font-bold text-white">
          {initials(business.name ?? '')}
        </span>
        <div className="min-w-0 flex-1">
          <p className="truncate font-semibold">{business.name}</p>
          <p className="text-xs text-muted">{platformName(platformCode)}</p>
        </div>
        {revision?.callToAction && revision.callToAction !== 'None' && <Badge tone="sun">{enumLabel(t, 'cta', revision.callToAction)}</Badge>}
      </header>

      {assets.length > 0 ? (
        <div className={assets.length > 1 ? 'grid grid-cols-2 gap-0.5' : ''}>
          {assets.slice(0, 4).map((asset) => (
            <MediaImage key={asset.assetId} contentUrl={asset.contentUrl} mimeType={asset.mimeType} alt={t('content.postImage')} className="aspect-square w-full" />
          ))}
        </div>
      ) : (
        details.visualIdea && (
          <div className="mx-4 rounded-2xl bg-linear-to-br from-brand-100 to-sun-300/30 p-4 text-sm text-brand-900 dark:from-brand-900 dark:to-brand-800 dark:text-brand-100">
            <p className="text-xs font-semibold opacity-70">{t('content.visualIdea')}</p>
            <p className="mt-1" dir="auto">{details.visualIdea}</p>
          </div>
        )
      )}

      <div className="flex gap-4 px-4 pt-3 text-muted" aria-hidden>
        <Heart className="size-5" />
        <MessageCircle className="size-5" />
        <Send className="size-5" />
      </div>

      <div className="p-4 pt-2">
        {revision?.body ? (
          <p className="leading-relaxed whitespace-pre-wrap [overflow-wrap:anywhere]" dir="auto">{revision.body}</p>
        ) : (
          <p className="text-muted">{t('content.noText')}</p>
        )}
        {hashtags.length > 0 && (
          <p className="mt-2 flex flex-wrap gap-x-2 text-sm font-semibold text-brand-600 dark:text-brand-400" dir="auto">
            {hashtags.map((tag) => <span key={tag}>{tag.startsWith('#') ? tag : `#${tag}`}</span>)}
          </p>
        )}
      </div>
    </article>
  )
}
