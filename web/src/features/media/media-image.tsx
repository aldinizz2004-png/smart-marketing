import { ImageOff } from 'lucide-react'
import { cn } from '@/lib/cn'
import { useMediaBlob } from './api'

type MediaImageProps = { contentUrl?: string | null; alt: string; className?: string; mimeType?: string | null }

export function MediaImage({ contentUrl, alt, className, mimeType }: MediaImageProps) {
  const blob = useMediaBlob(mimeType && !mimeType.startsWith('image/') ? null : contentUrl)
  if (blob.data) return <img src={blob.data} alt={alt} className={cn('object-cover', className)} />
  return (
    <div className={cn('grid place-items-center bg-surface-2 text-muted', blob.isPending && contentUrl ? 'animate-pulse' : '', className)} role="img" aria-label={alt}>
      {!blob.isPending || !contentUrl ? <ImageOff className="size-6" aria-hidden /> : null}
    </div>
  )
}
