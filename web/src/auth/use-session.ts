import { useSyncExternalStore } from 'react'
import { sessionStore, type Session } from './session'

export function useSessionState() {
  return useSyncExternalStore(sessionStore.subscribe, sessionStore.get, sessionStore.get)
}

// For pages behind RequireAuth, where a session always exists.
export function useSession(): Session {
  const state = useSessionState()
  if (state.status !== 'authenticated') throw new Error('useSession is only available behind RequireAuth.')
  return state.session
}
