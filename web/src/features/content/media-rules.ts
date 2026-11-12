import type { ContentFormat } from './api'

// Mirrors ContentService.MediaRules: how many files each format needs before review.
export const mediaRules: Record<ContentFormat, { min: number; max: number; kind: 'image' | 'video' | 'any' | null }> = {
  Text: { min: 0, max: 0, kind: null },
  SingleImage: { min: 1, max: 1, kind: 'image' },
  Carousel: { min: 2, max: 10, kind: 'image' },
  Video: { min: 1, max: 1, kind: 'video' },
  Reel: { min: 1, max: 1, kind: 'video' },
  Story: { min: 1, max: 1, kind: 'any' },
  Live: { min: 0, max: 0, kind: null },
}

export type MediaProblem = { key: 'needsMedia' | 'tooManyMedia' | 'noMediaAllowed'; min: number; max: number } | null

export function mediaProblem(format: ContentFormat | null | undefined, count: number): MediaProblem {
  const rule = mediaRules[format ?? 'Text']
  if (rule.max === 0 && count > 0) return { key: 'noMediaAllowed', min: 0, max: 0 }
  if (count < rule.min) return { key: 'needsMedia', min: rule.min, max: rule.max }
  if (count > rule.max) return { key: 'tooManyMedia', min: rule.min, max: rule.max }
  return null
}

// A sensible format for the number of images picked, used until the owner picks one themselves.
export function formatForImages(count: number): ContentFormat {
  return count === 0 ? 'Text' : count === 1 ? 'SingleImage' : 'Carousel'
}
