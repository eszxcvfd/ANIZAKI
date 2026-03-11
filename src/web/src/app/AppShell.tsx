import type { ReactNode } from 'react'
import { clearAuthSession, type AuthSession } from '../features/auth/session/authSession'

type AppShellProps = {
  title: string
  session: AuthSession | null
  children: ReactNode
}

export function AppShell({ session, children }: AppShellProps) {
  function handleSignOut() {
    clearAuthSession()
    window.location.assign('/')
  }

  return (
    <div className="app-shell min-h-screen">
      <header className="app-header">
        <div className="app-header__inner">
          <div className="flex items-center gap-8">
            <a href="/" className="flex items-center gap-2 text-xl font-bold tracking-tight text-slate-900 transition-opacity hover:opacity-80">
              <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-indigo-600 font-black text-white">A</span>
              Anizaki
            </a>
            <nav className="hidden items-center gap-6 md:flex">
              <a href="/" className="text-sm font-medium text-slate-600 hover:text-indigo-600 transition-colors">Home</a>
              {session?.role === 'admin' && (
                <a href="/admin/console" className="text-sm font-medium text-slate-600 hover:text-indigo-600 transition-colors">Admin</a>
              )}
            </nav>
          </div>
          
          <div className="flex items-center gap-4">
            {session ? (
              <div className="flex items-center gap-4">
                <div className="flex flex-col items-end">
                  <span className="text-xs font-semibold text-slate-900">{session.userEmail}</span>
                  <span className="text-[10px] uppercase tracking-wider font-bold text-indigo-600">{session.role}</span>
                </div>
                <a 
                  href="/profile" 
                  className="flex h-9 w-9 items-center justify-center rounded-full bg-white border border-slate-200 text-slate-600 hover:border-indigo-500 hover:text-indigo-600 transition-all shadow-sm"
                  title="Profile"
                >
                  <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M19 21v-2a4 4 0 0 0-4-4H9a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/></svg>
                </a>
                <button
                  className="btn-outline px-3 py-1.5 text-xs font-bold"
                  type="button"
                  onClick={handleSignOut}
                >
                  Sign out
                </button>
              </div>
            ) : (
              <div className="flex items-center gap-3">
                <a href="/auth/login" className="text-sm font-medium text-slate-600 hover:text-indigo-600 transition-colors">Login</a>
                <a href="/auth/register" className="btn-primary px-4 py-2 text-sm">Get Started</a>
              </div>
            )}
          </div>
        </div>
      </header>
      <main className="app-main animate-in fade-in slide-in-from-bottom-4 duration-700">{children}</main>
      <footer className="mt-auto border-t border-slate-200 py-8">
        <div className="mx-auto max-width-[1100px] px-6 text-center">
          <p className="text-xs text-slate-500 font-medium">© 2026 Anizaki Monorepo. Built with passion and precision.</p>
        </div>
      </footer>
    </div>
  )
}
