import { useCallback, useState } from 'react'

export type Theme = 'light' | 'dark'

const storageKey = 'smp.theme'

function currentTheme(): Theme {
  return document.documentElement.classList.contains('dark') ? 'dark' : 'light'
}

export function useTheme() {
  const [theme, setTheme] = useState<Theme>(currentTheme)

  const toggle = useCallback(() => {
    const next: Theme = currentTheme() === 'dark' ? 'light' : 'dark'
    document.documentElement.classList.toggle('dark', next === 'dark')
    try {
      localStorage.setItem(storageKey, next)
    } catch {
      // private mode: the choice just lasts for this visit
    }
    setTheme(next)
  }, [])

  return { theme, toggle }
}
