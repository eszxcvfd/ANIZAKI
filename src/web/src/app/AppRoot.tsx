import { useEffect, useState } from 'react'
import { AppShell } from './AppShell'
import { resolveRoute } from './routes'
import { readAuthSession, type AuthSession } from '../features/auth/session/authSession'

export function AppRoot() {
  const [path, setPath] = useState(window.location.pathname)
  const [session, setSession] = useState<AuthSession | null>(() => readAuthSession())

  useEffect(() => {
    const onPopState = () => setPath(window.location.pathname)
    const onStorage = () => setSession(readAuthSession())

    window.addEventListener('popstate', onPopState)
    window.addEventListener('storage', onStorage)

    return () => {
      window.removeEventListener('popstate', onPopState)
      window.removeEventListener('storage', onStorage)
    }
  }, [])

  const route = resolveRoute(path, session)
  const CurrentPage = route.component

  return (
    <AppShell title={route.title} session={session}>
      <CurrentPage />
    </AppShell>
  )
}
