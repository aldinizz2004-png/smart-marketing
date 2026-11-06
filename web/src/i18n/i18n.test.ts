import { afterEach, describe, expect, it } from 'vitest'
import i18n, { changeLanguage, storedLanguage } from '@/i18n'
import ar from './locales/ar.json'
import en from './locales/en.json'

function keys(value: object, prefix = ''): string[] {
  return Object.entries(value).flatMap(([key, child]) =>
    typeof child === 'object' && child !== null ? keys(child, `${prefix}${key}.`) : [`${prefix}${key}`],
  )
}

describe('i18n', () => {
  afterEach(async () => {
    await changeLanguage('ar')
  })

  it('starts in Arabic, right to left', () => {
    expect(i18n.language).toBe('ar')
    expect(document.documentElement.dir).toBe('rtl')
    expect(document.documentElement.lang).toBe('ar')
  })

  it('switches to English, left to right, and remembers it', async () => {
    await changeLanguage('en')

    expect(i18n.t('nav.home')).toBe('Home')
    expect(document.documentElement.dir).toBe('ltr')
    expect(storedLanguage()).toBe('en')
  })

  it('has the same keys in Arabic and English', () => {
    expect(keys(en).sort()).toEqual(keys(ar).sort())
  })
})
