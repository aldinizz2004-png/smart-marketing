import type { TFunction } from 'i18next'

// Enum values stay in English for the API; these give the words people read.
export const enumLabel = (t: TFunction, group: 'contentType' | 'format' | 'hook' | 'cta' | 'objective' | 'metric', value?: string | null) =>
  value ? t(`enums.${group}.${value}`, { defaultValue: value }) : '—'
