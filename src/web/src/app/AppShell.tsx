import type { ReactNode } from 'react'
import { clearAuthSession, type AuthSession } from '../features/auth/session/authSession'

type AppShellProps = {
  title: string
  session: AuthSession | null
  children: ReactNode
}

export function AppShell({ title, session, children }: AppShellProps) {
  function handleSignOut() {
    clearAuthSession()
    window.location.assign('/')
  }

  return (
    <div className="app-shell min-h-screen bg-slate-100 text-slate-800">
      <header className="app-header border-b border-sky-900">
        <div className="app-header__inner">
          <strong>
            <a href="/">Anizaki</a>
          </strong>
          <nav className="flex items-center gap-3 text-sm">
            <a href="/">Home</a>
            {session ? <a href="/profile">Profile</a> : <a href="/auth/login">Login</a>}
            {session?.role === 'admin' ? <a href="/admin/console">Admin</a> : null}
            <span className="rounded bg-sky-950 px-2 py-1 text-xs">
              {session ? `${session.role}:${session.userEmail}` : 'anonymous'}
            </span>
            {session ? (
              <button
                className="rounded border border-slate-300 px-2 py-1 text-xs"
                type="button"
                onClick={handleSignOut}
              >
                Sign out
              </button>
            ) : null}
          </nav>
          <span>{title}</span>
        </div>
      </header>
      <main className="app-main px-4 py-6">{children}</main>
    </div>
  )
}
