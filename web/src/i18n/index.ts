import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import ar from './locales/ar.json'
import en from './locales/en.json'

export type Language = 'ar' | 'en'

const storageKey = 'smp.lang'

export function storedLanguage(): Language {
  try {
    return localStorage.getItem(storageKey) === 'en' ? 'en' : 'ar'
  } catch {
    return 'ar'
  }
}

export function applyDocumentLanguage(language: Language) {
  document.documentElement.lang = language
  document.documentElement.dir = language === 'ar' ? 'rtl' : 'ltr'
}

export async function changeLanguage(language: Language) {
  try {
    localStorage.setItem(storageKey, language)
  } catch {
    // private mode: the choice just lasts for this visit
  }
  applyDocumentLanguage(language)
  await i18n.changeLanguage(language)
}

const initial = storedLanguage()
applyDocumentLanguage(initial)

void i18n.use(initReactI18next).init({
  resources: { ar: { translation: ar }, en: { translation: en } },
  lng: initial,
  fallbackLng: 'ar',
  interpolation: { escapeValue: false },
})

export default i18n
