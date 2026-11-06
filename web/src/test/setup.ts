import '@testing-library/jest-dom/vitest'
import { cleanup } from '@testing-library/react'
import { afterEach } from 'vitest'
import { sessionStore } from '@/auth/session'
import { changeLanguage } from '@/i18n'

afterEach(async () => {
  cleanup()
  vi.unstubAllGlobals()
  localStorage.clear()
  sessionStore.set({ status: 'loading' })
  await changeLanguage('ar')
})
