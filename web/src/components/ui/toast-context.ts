import { createContext, useContext } from 'react'

export type ToastApi = {
  success: (title: string, detail?: string) => void
  error: (error: unknown) => void
}

export const ToastContext = createContext<ToastApi | null>(null)

export function useToast(): ToastApi {
  const api = useContext(ToastContext)
  if (!api) throw new Error('useToast needs a ToastProvider.')
  return api
}
